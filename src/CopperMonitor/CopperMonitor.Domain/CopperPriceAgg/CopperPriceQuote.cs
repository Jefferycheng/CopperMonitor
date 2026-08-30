namespace CopperMonitor.Domain.CopperPriceAgg;

/// <summary>
/// One trading day's copper closing price, in USD per pound (COMEX HG convention).
/// </summary>
public record CopperPriceQuote(DateOnly Date, decimal CloseUsdPerLb);
