using System.Text.Json;
using CopperMonitor.Application.Services;

namespace CopperMonitor.Api.Apis;

/// <summary>Receives LINE webhook events; parsing/reply logic lives in LineWebhookHandler.</summary>
public static class LineWebhookApi
{
    public static IEndpointRouteBuilder MapLineWebhookApiV1(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/line/webhook", async (HttpContext context, LineWebhookHandler handler) =>
        {
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
            await handler.HandleAsync(doc, context.RequestAborted);
            // LINE requires a 200 response; verification pings have an empty events array.
            return Results.Ok();
        });
        return app;
    }
}
