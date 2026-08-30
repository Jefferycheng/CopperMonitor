using uServeCore.CopperMonitor.Domain.CopperPriceAgg;

namespace uServeCore.CopperMonitor.Application.ExternalService;

public interface IExchangeRateProvider
{
    /// <summary>Daily USD/TWD closing rates for [from, to], ascending by date.</summary>
    Task<IReadOnlyList<ExchangeRateQuote>> GetUsdTwdRatesAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
