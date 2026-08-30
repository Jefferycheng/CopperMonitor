using System.Text.Json;
using CopperMonitor.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CopperMonitor.Functions.Functions;

public class LineWebhookFunction(LineWebhookHandler handler)
{
    [Function("LineWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/line/webhook")] HttpRequest req)
    {
        using var doc = await JsonDocument.ParseAsync(req.Body, cancellationToken: req.HttpContext.RequestAborted);
        await handler.HandleAsync(doc, req.HttpContext.RequestAborted);
        // LINE requires a 200 response; verification pings have an empty events array.
        return new OkResult();
    }
}
