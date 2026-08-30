using MediatR;
using CopperMonitor.Application.Commands.CopperReport;

namespace CopperMonitor.Api.BackgroundServices;

public class ReportScheduleOptions
{
    public const string SectionName = "ReportSchedule";

    /// <summary>Local send time in the configured time zone, HH:mm.</summary>
    public string LocalTime { get; set; } = "08:30";

    public string TimeZone { get; set; } = "Asia/Taipei";

    /// <summary>Skip Saturday/Sunday (markets are closed).</summary>
    public bool WeekdaysOnly { get; set; } = true;

    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Sends the copper report to LINE every weekday at the configured local time (default 08:30 Asia/Taipei).
/// </summary>
public class DailyCopperReportService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyCopperReportService> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = configuration.GetSection(ReportScheduleOptions.SectionName).Get<ReportScheduleOptions>()
                      ?? new ReportScheduleOptions();

        if (!options.Enabled)
        {
            logger.LogInformation("Daily copper report scheduler is disabled.");
            return;
        }

        var timeZone = ResolveTimeZone(options.TimeZone);
        var sendTime = TimeOnly.ParseExact(options.LocalTime, "HH:mm");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTimeOffset.UtcNow, timeZone, sendTime, options.WeekdaysOnly);
            logger.LogInformation("Next copper report scheduled in {Delay} (at {Time} {Zone})",
                delay, options.LocalTime, options.TimeZone);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new SendDailyReportCommand(), stoppingToken);
                logger.LogInformation("Daily copper report sent for {Date}", result.Data?.Date);
            }
            catch (Exception ex)
            {
                // Log and keep the loop alive — the next day's run must still fire.
                logger.LogError(ex, "Daily copper report failed");
            }
        }
    }

    internal static TimeSpan GetDelayUntilNextRun(DateTimeOffset utcNow, TimeZoneInfo timeZone, TimeOnly sendTime, bool weekdaysOnly)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var candidate = localNow.Date.Add(sendTime.ToTimeSpan());
        if (candidate <= localNow.DateTime)
            candidate = candidate.AddDays(1);

        while (weekdaysOnly && candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            candidate = candidate.AddDays(1);

        var candidateUtc = TimeZoneInfo.ConvertTimeToUtc(candidate, timeZone);
        return candidateUtc - utcNow.UtcDateTime;
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        // IANA id works on macOS/Linux; fall back to the Windows id.
        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz)
            ? tz
            : TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
    }
}
