namespace CopperMonitor.Application.ExternalService;

public interface ILineMessenger
{
    /// <summary>Push a plain-text message to the configured LINE group.</summary>
    Task PushTextAsync(string text, CancellationToken ct = default);

    /// <summary>Reply to an incoming message using its reply token (works without a configured group ID).</summary>
    Task ReplyTextAsync(string replyToken, string text, CancellationToken ct = default);
}
