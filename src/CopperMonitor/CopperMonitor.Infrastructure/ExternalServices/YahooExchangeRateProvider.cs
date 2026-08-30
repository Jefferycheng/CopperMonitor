using Microsoft.Extensions.Logging;
using CopperMonitor.Application.ExternalService;
using CopperMonitor.Domain.CopperPriceAgg;
using CopperMonitor.Domain.SeedWork;

namespace CopperMonitor.Infrastructure.ExternalServices;

/// <summary>USD/TWD daily rate (Yahoo symbol TWD=X).</summary>
public class YahooExchangeRateProvider(HttpClient httpClient, ILogger<YahooExchangeRateProvider> logger)
    : IExchangeRateProvider
{
    private const string Symbol = "TWD=X";

    public async Task<IReadOnlyList<ExchangeRateQuote>> GetUsdTwdRatesAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var closes = await YahooChartClient.GetDailyClosesAsync(
            httpClient, Symbol, from, to, CopperExceptionCode.ExchangeRateUnavailable, logger, ct);
        return closes.Select(c => new ExchangeRateQuote(c.Date, c.Close)).ToList();
    }
}
