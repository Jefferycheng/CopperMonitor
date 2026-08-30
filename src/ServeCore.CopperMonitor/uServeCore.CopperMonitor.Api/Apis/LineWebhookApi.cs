using System.Text.Json;
using uServeCore.CopperMonitor.Application.ExternalService;
using uServeCore.CopperMonitor.Application.Services;

namespace uServeCore.CopperMonitor.Api.Apis;

/// <summary>
/// Receives LINE webhook events.
/// - Logs the source id (use it to discover the group ID for Line__GroupId).
/// - Chat commands: "today"/"報告" → today's report; a date or date range → historical prices.
/// </summary>
public static class LineWebhookApi
{
    public static IEndpointRouteBuilder MapLineWebhookApiV1(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/v1/line/webhook", async (
            HttpContext context,
            ChatCommandService chatCommands,
            ILineMessenger lineMessenger,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("LineWebhook");
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);

            if (doc.RootElement.TryGetProperty("events", out var events))
            {
                foreach (var e in events.EnumerateArray())
                {
                    LogSource(e, logger);
                    await TryReplyAsync(e, chatCommands, lineMessenger, logger, context.RequestAborted);
                }
            }

            // LINE requires a 200 response; verification pings have an empty events array.
            return Results.Ok();
        });
        return app;
    }

    private static void LogSource(JsonElement e, ILogger logger)
    {
        if (!e.TryGetProperty("source", out var source)) return;
        var type = source.TryGetProperty("type", out var t) ? t.GetString() : "unknown";
        var id = type switch
        {
            "group" => source.GetProperty("groupId").GetString(),
            "room" => source.GetProperty("roomId").GetString(),
            "user" => source.GetProperty("userId").GetString(),
            _ => null
        };
        logger.LogInformation(">>> LINE webhook: source type={Type}, id={Id} — for a group, put this id into Line__GroupId", type, id);
    }

    private static async Task TryReplyAsync(
        JsonElement e, ChatCommandService chatCommands, ILineMessenger lineMessenger, ILogger logger, CancellationToken ct)
    {
        if (e.GetProperty("type").GetString() != "message") return;
        if (!e.TryGetProperty("message", out var message) || message.GetProperty("type").GetString() != "text") return;
        if (!e.TryGetProperty("replyToken", out var tokenElement)) return;

        var text = message.GetProperty("text").GetString() ?? string.Empty;
        var replyToken = tokenElement.GetString() ?? string.Empty;

        try
        {
            var reply = await chatCommands.TryHandleAsync(text, ct);
            if (reply is not null)
                await lineMessenger.ReplyTextAsync(replyToken, reply, ct);
        }
        catch (Exception ex)
        {
            // Never fail the webhook — LINE would retry and disable the endpoint on repeated errors.
            logger.LogError(ex, "Failed to handle chat command '{Text}'", text);
        }
    }
}
