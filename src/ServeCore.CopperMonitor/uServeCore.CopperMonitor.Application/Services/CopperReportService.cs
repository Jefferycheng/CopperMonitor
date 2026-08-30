using System.Globalization;
using Microsoft.Extensions.Options;
using uServeCore.CopperMonitor.Application.Configs;
using uServeCore.CopperMonitor.Application.DTO.CopperPrice;
using uServeCore.CopperMonitor.Application.ExternalService;
using uServeCore.CopperMonitor.Domain.CopperPriceAgg;
using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Application.Services;

/// <summary>
/// Builds the daily copper report entirely from on-demand provider queries — no local storage.
/// </summary>
public class CopperReportService(
    ICopperPriceProvider priceProvider,
    IExchangeRateProvider rateProvider,
    IOptions<AlertOptions> alertOptions)
{
    // Enough calendar days to cover the previous trading day and the 7-days-ago baseline
    // across weekends and holidays.
    private const int LookbackDays = 21;

    public async Task<CopperPriceReportDto> BuildReportAsync(DateOnly asOf, CancellationToken ct = default)
    {
        var from = asOf.AddDays(-LookbackDays);

        var prices = await priceProvider.GetDailyPricesAsync(from, asOf, ct);
        if (prices.Count == 0)
            throw new CopperDomainException(CopperExceptionCode.PriceDataUnavailable,
                $"No copper prices available between {from:yyyy-MM-dd} and {asOf:yyyy-MM-dd}.");

        var rates = await rateProvider.GetUsdTwdRatesAsync(from, asOf, ct);
        if (rates.Count == 0)
            throw new CopperDomainException(CopperExceptionCode.ExchangeRateUnavailable,
                $"No USD/TWD rates available between {from:yyyy-MM-dd} and {asOf:yyyy-MM-dd}.");

        var latest = prices[^1];
        var rate = rates[^1].UsdToTwd;
        var dailyChange = PriceChangeCalculator.DailyChangePercent(prices);
        var weeklyChange = PriceChangeCalculator.WeeklyChangePercent(prices);
        var twdPerTon = Math.Round(PriceChangeCalculator.UsdPerLbToTwdPerTon(latest.CloseUsdPerLb, rate), 0);

        var alerts = alertOptions.Value;
        var dailyAlert = dailyChange.HasValue && Math.Abs(dailyChange.Value) >= alerts.DailyChangeThresholdPercent;
        var weeklyAlert = weeklyChange.HasValue && Math.Abs(weeklyChange.Value) >= alerts.WeeklyChangeThresholdPercent;

        var dto = new CopperPriceReportDto
        {
            Date = latest.Date.ToString("yyyy-MM-dd"),
            CloseUsdPerLb = latest.CloseUsdPerLb,
            DailyChangePercent = dailyChange,
            WeeklyChangePercent = weeklyChange,
            UsdToTwd = rate,
            TwdPerTon = twdPerTon,
            DailyAlert = dailyAlert,
            WeeklyAlert = weeklyAlert
        };
        dto.ReportText = FormatReport(dto, alerts);
        return dto;
    }

    private static string FormatReport(CopperPriceReportDto r, AlertOptions alerts)
    {
        var lines = new List<string>
        {
            $"📊 銅價日報 {r.Date} (COMEX HG)",
            $"收盤價：{r.CloseUsdPerLb.ToString("0.####", CultureInfo.InvariantCulture)} USD/lb",
            $"日漲跌：{FormatChange(r.DailyChangePercent)}",
            $"週漲跌：{FormatChange(r.WeeklyChangePercent)}",
            $"USD/TWD：{r.UsdToTwd.ToString("0.###", CultureInfo.InvariantCulture)}",
            $"台幣換算：{r.TwdPerTon.ToString("N0", CultureInfo.InvariantCulture)} TWD/噸"
        };

        if (r.DailyAlert)
            lines.Add($"⚠️ 警示：日變動超過 {alerts.DailyChangeThresholdPercent}%");
        if (r.WeeklyAlert)
            lines.Add($"⚠️ 警示：週變動超過 {alerts.WeeklyChangeThresholdPercent}%");

        return string.Join("\n", lines);
    }

    private static string FormatChange(decimal? percent)
    {
        if (!percent.HasValue) return "N/A";
        var arrow = percent.Value > 0 ? "▲" : percent.Value < 0 ? "▼" : "—";
        return $"{(percent.Value >= 0 ? "+" : "")}{percent.Value.ToString("0.##", CultureInfo.InvariantCulture)}% {arrow}";
    }
}
