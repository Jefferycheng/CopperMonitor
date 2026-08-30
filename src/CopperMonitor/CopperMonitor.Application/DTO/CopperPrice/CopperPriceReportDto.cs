namespace CopperMonitor.Application.DTO.CopperPrice;

public class CopperPriceReportDto
{
    /// <summary>Trading date of the latest quote, yyyy-MM-dd.</summary>
    public string Date { get; set; } = string.Empty;
    public decimal CloseUsdPerLb { get; set; }
    public decimal? DailyChangePercent { get; set; }
    public decimal? WeeklyChangePercent { get; set; }
    public decimal UsdToTwd { get; set; }
    public decimal TwdPerTon { get; set; }
    public bool DailyAlert { get; set; }
    public bool WeeklyAlert { get; set; }
    /// <summary>The formatted text pushed to LINE.</summary>
    public string ReportText { get; set; } = string.Empty;
}
