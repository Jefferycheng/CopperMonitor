using MediatR;
using uServeCore.CopperMonitor.Application.Commands.CopperReport;
using uServeCore.CopperMonitor.Application.Queries.CopperPrice;
using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Api.Apis;

public static class CopperPriceApi
{
    public static IEndpointRouteBuilder MapCopperPriceApiV1(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/copper-price").WithTags("copper-price");

        // Latest report (same content as the daily LINE message).
        group.MapGet("/report", async (ISender sender, DateOnly? date) =>
            TypedResults.Ok(await sender.Send(new GetCopperPriceReportQuery { Date = date })));

        // Historical prices: ?date=2026-08-01 for one day, or ?from=...&to=... for a range.
        group.MapGet("/history", async (ISender sender, DateOnly? date, DateOnly? from, DateOnly? to) =>
        {
            var query = date.HasValue
                ? new GetCopperPriceHistoryQuery { From = date.Value, To = date.Value }
                : new GetCopperPriceHistoryQuery
                {
                    From = from ?? throw new CopperDomainException(CopperExceptionCode.InvalidDateRange,
                        "Provide either 'date' or both 'from' and 'to'."),
                    To = to ?? throw new CopperDomainException(CopperExceptionCode.InvalidDateRange,
                        "Provide either 'date' or both 'from' and 'to'.")
                };
            return TypedResults.Ok(await sender.Send(query));
        });

        // Manually trigger the LINE push (same as the 08:30 scheduled job).
        group.MapPost("/report/send", async (ISender sender) =>
            TypedResults.Ok(await sender.Send(new SendDailyReportCommand())));

        return app;
    }
}
