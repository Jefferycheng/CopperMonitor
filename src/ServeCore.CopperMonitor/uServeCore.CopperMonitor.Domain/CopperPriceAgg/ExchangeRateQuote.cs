namespace uServeCore.CopperMonitor.Domain.CopperPriceAgg;

/// <summary>One day's USD/TWD closing exchange rate.</summary>
public record ExchangeRateQuote(DateOnly Date, decimal UsdToTwd);
