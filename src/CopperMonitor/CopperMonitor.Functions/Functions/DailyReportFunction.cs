using CopperMonitor.Application.Commands.CopperReport;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CopperMonitor.Functions.Functions;

public class DailyReportFunction(ISender sender, ILogger<DailyReportFunction> logger)
{
    /// <summary>
    /// Weekdays at 00:30 UTC = 08:30 Asia/Taipei (Taiwan has no DST, so a fixed UTC cron is safe
    /// on every hosting plan). Override with the ReportScheduleCron app setting.
    /// </summary>
    [Function("DailyCopperReport")]
    public async Task Run([TimerTrigger("%ReportScheduleCron%")] TimerInfo timer)
    {
        try
        {
            var result = await sender.Send(new SendDailyReportCommand());
            logger.LogInformation("Daily copper report sent for {Date}", result.Data?.Date);
        }
        catch (Exception ex)
        {
            // Log and swallow — the next scheduled run must still fire.
            logger.LogError(ex, "Daily copper report failed");
        }
    }
}
