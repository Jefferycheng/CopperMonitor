using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Domain.CopperPriceAgg;

public static class PriceChangeCalculator
{
    // Metric ton = 2204.62262185 lb (COMEX quotes copper in USD/lb).
    public const decimal PoundsPerMetricTon = 2204.62262185m;

    public static decimal UsdPerLbToTwdPerTon(decimal usdPerLb, decimal usdToTwd)
        => usdPerLb * PoundsPerMetricTon * usdToTwd;

    /// <summary>
    /// Percent change between the latest quote and the previous trading day.
    /// Returns null when the series has fewer than two points.
    /// </summary>
    public static decimal? DailyChangePercent(IReadOnlyList<CopperPriceQuote> ascendingSeries)
    {
        if (ascendingSeries.Count < 2) return null;
        var latest = ascendingSeries[^1];
        var previous = ascendingSeries[^2];
        return ChangePercent(previous.CloseUsdPerLb, latest.CloseUsdPerLb);
    }

    /// <summary>
    /// Percent change between the latest quote and the most recent trading day
    /// on or before 7 calendar days earlier. Returns null when no such day exists.
    /// </summary>
    public static decimal? WeeklyChangePercent(IReadOnlyList<CopperPriceQuote> ascendingSeries)
    {
        if (ascendingSeries.Count < 2) return null;
        var latest = ascendingSeries[^1];
        var target = latest.Date.AddDays(-7);
        var baseline = ascendingSeries
            .Where(q => q.Date <= target)
            .OrderBy(q => q.Date)
            .LastOrDefault();
        return baseline is null ? null : ChangePercent(baseline.CloseUsdPerLb, latest.CloseUsdPerLb);
    }

    private static decimal ChangePercent(decimal from, decimal to)
    {
        if (from == 0)
            throw new CopperDomainException(CopperExceptionCode.PriceDataUnavailable, "Baseline price is zero.");
        return Math.Round((to - from) / from * 100m, 2);
    }
}
