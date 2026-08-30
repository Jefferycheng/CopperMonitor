namespace CopperMonitor.Application.DTO.CopperPrice;

public class CopperPriceDayDto
{
    /// <summary>Trading date, yyyy-MM-dd.</summary>
    public string Date { get; set; } = string.Empty;
    public decimal CloseUsdPerLb { get; set; }
    /// <summary>USD/TWD rate on that day; null when no rate quote exists for the date.</summary>
    public decimal? UsdToTwd { get; set; }
    public decimal? TwdPerTon { get; set; }
}
