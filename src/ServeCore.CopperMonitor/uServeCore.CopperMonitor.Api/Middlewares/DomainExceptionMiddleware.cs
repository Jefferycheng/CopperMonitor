using uServeCore.CopperMonitor.Application.Abstractions;
using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Api.Middlewares;

/// <summary>Converts CopperDomainException into the ResponseResult failure envelope.</summary>
public class DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (CopperDomainException ex)
        {
            logger.LogWarning(ex, "Domain error {Code}", ex.Code);
            context.Response.StatusCode = ex.Code switch
            {
                CopperExceptionCode.InvalidDateRange => StatusCodes.Status400BadRequest,
                CopperExceptionCode.LineNotConfigured => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status502BadGateway
            };
            await context.Response.WriteAsJsonAsync(ResponseResult<object>.Failure(new ResponseError
            {
                Code = ex.Code.ToString(),
                Message = ex.Message
            }));
        }
    }
}
