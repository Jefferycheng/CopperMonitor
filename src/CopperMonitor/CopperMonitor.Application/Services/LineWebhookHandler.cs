using System.Text.Json;
using Microsoft.Extensions.Logging;
using CopperMonitor.Application.ExternalService;

namespace CopperMonitor.Application.Services;

/// <summary>
/// Processes a LINE webhook payload: logs each event's source id (used to discover the
/// group ID for Line__GroupId) and answers chat commands via ChatCommandService.
/// Never throws — LINE retries and eventually disables webhooks that return errors.
/// </summary>
public class LineWebhookHandler(
    ChatCommandService chatCommands,
    ILineMessenger lineMessenger,
    ILogger<LineWebhookHandler> logger)
{
    public async Task HandleAsync(JsonDocument payload, CancellationToken ct = default)
    {
        if (!payload.RootElement.TryGetProperty("events", out var events)) return;

        foreach (var e in events.EnumerateArray())
        {
            LogSource(e);
            await TryReplyAsync(e, ct);
        }
    }

    private void LogSource(JsonElement e)
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

    private async Task TryReplyAsync(JsonElement e, CancellationToken ct)
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
            logger.LogError(ex, "Failed to handle chat command '{Text}'", text);
        }
    }
}
