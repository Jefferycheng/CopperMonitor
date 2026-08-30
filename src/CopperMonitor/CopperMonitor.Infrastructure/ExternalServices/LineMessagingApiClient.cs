using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CopperMonitor.Application.ExternalService;
using CopperMonitor.Domain.SeedWork;

namespace CopperMonitor.Infrastructure.ExternalServices;

public class LineOptions
{
    public const string SectionName = "Line";

    /// <summary>LINE Messaging API channel access token. Set via env var Line__ChannelAccessToken.</summary>
    public string ChannelAccessToken { get; set; } = string.Empty;

    /// <summary>LINE group ID to push to. Set via env var Line__GroupId.</summary>
    public string GroupId { get; set; } = string.Empty;
}

public class LineMessagingApiClient(
    HttpClient httpClient,
    IOptions<LineOptions> options,
    ILogger<LineMessagingApiClient> logger) : ILineMessenger
{
    public async Task PushTextAsync(string text, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ChannelAccessToken) || string.IsNullOrWhiteSpace(opts.GroupId))
            throw new CopperDomainException(CopperExceptionCode.LineNotConfigured,
                "LINE is not configured. Set Line__ChannelAccessToken and Line__GroupId environment variables.");

        await SendMessageAsync("v2/bot/message/push", new
        {
            to = opts.GroupId,
            messages = new[] { new { type = "text", text } }
        }, "push", ct);
        logger.LogInformation("LINE push delivered to group {GroupId}", opts.GroupId);
    }

    public async Task ReplyTextAsync(string replyToken, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ChannelAccessToken))
            throw new CopperDomainException(CopperExceptionCode.LineNotConfigured,
                "LINE is not configured. Set Line__ChannelAccessToken environment variable.");

        await SendMessageAsync("v2/bot/message/reply", new
        {
            replyToken,
            messages = new[] { new { type = "text", text } }
        }, "reply", ct);
        logger.LogInformation("LINE reply delivered");
    }

    private async Task SendMessageAsync(string path, object payload, string operation, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ChannelAccessToken);
        request.Content = JsonContent.Create(payload);

        try
        {
            var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("LINE {Operation} failed with {StatusCode}: {Body}", operation, (int)response.StatusCode, body);
                throw new CopperDomainException(CopperExceptionCode.LineDeliveryFailed,
                    $"LINE {operation} failed with status {(int)response.StatusCode}.");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "LINE {Operation} failed", operation);
            throw new CopperDomainException(CopperExceptionCode.LineDeliveryFailed,
                $"LINE {operation} failed: {ex.Message}");
        }
    }
}
