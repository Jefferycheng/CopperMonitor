using uServeCore.CopperMonitor.Domain.CopperPriceAgg;

namespace uServeCore.CopperMonitor.Application.ExternalService;

public interface ICopperPriceProvider
{
    /// <summary>Daily closing prices (USD/lb) for [from, to], ascending by date. Non-trading days are absent.</summary>
    Task<IReadOnlyList<CopperPriceQuote>> GetDailyPricesAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
