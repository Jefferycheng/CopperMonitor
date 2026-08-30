using uServeCore.CopperMonitor.Application.Abstractions;
using uServeCore.CopperMonitor.Application.DTO.CopperPrice;
using uServeCore.CopperMonitor.Application.ExternalService;
using uServeCore.CopperMonitor.Domain.CopperPriceAgg;
using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Application.Queries.CopperPrice;

/// <summary>
/// Historical prices for a single date (From == To) or a date range.
/// Queried live from the provider — nothing is stored locally.
/// </summary>
public class GetCopperPriceHistoryQuery : IQuery<ResponseResult<List<CopperPriceDayDto>>>
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
}

public class GetCopperPriceHistoryQueryHandler(
    ICopperPriceProvider priceProvider,
    IExchangeRateProvider rateProvider)
    : IQueryHandler<GetCopperPriceHistoryQuery, ResponseResult<List<CopperPriceDayDto>>>
{
    private const int MaxRangeDays = 366;

    public async Task<ResponseResult<List<CopperPriceDayDto>>> Handle(GetCopperPriceHistoryQuery request, CancellationToken ct)
    {
        if (request.From > request.To)
            throw new CopperDomainException(CopperExceptionCode.InvalidDateRange, "'from' must be on or before 'to'.");
        if (request.To.DayNumber - request.From.DayNumber > MaxRangeDays)
            throw new CopperDomainException(CopperExceptionCode.InvalidDateRange, $"Date range cannot exceed {MaxRangeDays} days.");

        var prices = await priceProvider.GetDailyPricesAsync(request.From, request.To, ct);
        var rates = await rateProvider.GetUsdTwdRatesAsync(request.From, request.To, ct);
        var rateByDate = rates.ToDictionary(r => r.Date, r => r.UsdToTwd);

        var days = prices.Select(p =>
        {
            var rate = rateByDate.TryGetValue(p.Date, out var r) ? r : (decimal?)null;
            return new CopperPriceDayDto
            {
                Date = p.Date.ToString("yyyy-MM-dd"),
                CloseUsdPerLb = p.CloseUsdPerLb,
                UsdToTwd = rate,
                TwdPerTon = rate.HasValue
                    ? Math.Round(PriceChangeCalculator.UsdPerLbToTwdPerTon(p.CloseUsdPerLb, rate.Value), 0)
                    : null
            };
        }).ToList();

        return ResponseResult<List<CopperPriceDayDto>>.Success(days);
    }
}
