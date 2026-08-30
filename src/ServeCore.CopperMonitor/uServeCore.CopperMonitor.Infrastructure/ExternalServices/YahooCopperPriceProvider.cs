using Microsoft.Extensions.Logging;
using uServeCore.CopperMonitor.Application.ExternalService;
using uServeCore.CopperMonitor.Domain.CopperPriceAgg;
using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Infrastructure.ExternalServices;

/// <summary>COMEX copper futures (Yahoo symbol HG=F), quoted in USD/lb.</summary>
public class YahooCopperPriceProvider(HttpClient httpClient, ILogger<YahooCopperPriceProvider> logger)
    : ICopperPriceProvider
{
    private const string Symbol = "HG=F";

    public async Task<IReadOnlyList<CopperPriceQuote>> GetDailyPricesAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var closes = await YahooChartClient.GetDailyClosesAsync(
            httpClient, Symbol, from, to, CopperExceptionCode.PriceDataUnavailable, logger, ct);
        return closes.Select(c => new CopperPriceQuote(c.Date, c.Close)).ToList();
    }
}
