namespace CopperMonitor.Application.Configs;

public class AlertOptions
{
    public const string SectionName = "Alert";

    /// <summary>Absolute daily change % that triggers an alert line in the report.</summary>
    public decimal DailyChangeThresholdPercent { get; set; } = 3m;

    /// <summary>Absolute weekly change % that triggers an alert line in the report.</summary>
    public decimal WeeklyChangeThresholdPercent { get; set; } = 5m;
}
