using CopperMonitor.Application.Abstractions;
using CopperMonitor.Application.Commands.CopperReport;
using CopperMonitor.Application.Queries.CopperPrice;
using CopperMonitor.Domain.SeedWork;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CopperMonitor.Functions.Functions;

public class CopperPriceFunctions(ISender sender, ILogger<CopperPriceFunctions> logger)
{
    [Function("GetReport")]
    public Task<IActionResult> GetReport(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/copper-price/report")] HttpRequest req)
        => Envelope(() => sender.Send(new GetCopperPriceReportQuery { Date = ParseDate(req.Query["date"]) }));

    [Function("GetHistory")]
    public Task<IActionResult> GetHistory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/copper-price/history")] HttpRequest req)
        => Envelope(() =>
        {
            var date = ParseDate(req.Query["date"]);
            var query = date.HasValue
                ? new GetCopperPriceHistoryQuery { From = date.Value, To = date.Value }
                : new GetCopperPriceHistoryQuery
                {
                    From = ParseDate(req.Query["from"]) ?? throw new CopperDomainException(
                        CopperExceptionCode.InvalidDateRange, "Provide either 'date' or both 'from' and 'to'."),
                    To = ParseDate(req.Query["to"]) ?? throw new CopperDomainException(
                        CopperExceptionCode.InvalidDateRange, "Provide either 'date' or both 'from' and 'to'.")
                };
            return sender.Send(query);
        });

    [Function("SendReport")]
    public Task<IActionResult> SendReport(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/copper-price/report/send")] HttpRequest req)
        => Envelope(() => sender.Send(new SendDailyReportCommand()));

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var d) ? d : null;

    // Mirrors the Api project's DomainExceptionMiddleware: domain errors → ResponseResult failure envelope.
    private async Task<IActionResult> Envelope<T>(Func<Task<T>> action)
    {
        try
        {
            return new OkObjectResult(await action());
        }
        catch (CopperDomainException ex)
        {
            logger.LogWarning(ex, "Domain error {Code}", ex.Code);
            var status = ex.Code switch
            {
                CopperExceptionCode.InvalidDateRange => StatusCodes.Status400BadRequest,
                CopperExceptionCode.LineNotConfigured => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status502BadGateway
            };
            return new ObjectResult(ResponseResult<object>.Failure(new ResponseError
            {
                Code = ex.Code.ToString(),
                Message = ex.Message
            }))
            { StatusCode = status };
        }
    }
}
