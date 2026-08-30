using System.Text.Json;
using Microsoft.Extensions.Logging;
using CopperMonitor.Domain.SeedWork;

namespace CopperMonitor.Infrastructure.ExternalServices;

/// <summary>
/// Shared client for the Yahoo Finance chart API
/// (https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?period1=..&period2=..&interval=1d).
/// Free and keyless; requires a browser-like User-Agent header.
/// </summary>
public static class YahooChartClient
{
    public static async Task<IReadOnlyList<(DateOnly Date, decimal Close)>> GetDailyClosesAsync(
        HttpClient httpClient, string symbol, DateOnly from, DateOnly to,
        CopperExceptionCode failureCode, ILogger logger, CancellationToken ct)
    {
        var period1 = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var period2 = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var url = $"v8/finance/chart/{Uri.EscapeDataString(symbol)}?period1={period1}&period2={period2}&interval=1d";

        string json;
        try
        {
            json = await httpClient.GetStringAsync(url, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Yahoo chart request failed for {Symbol} ({From} - {To})", symbol, from, to);
            throw new CopperDomainException(failureCode, $"Price source unavailable for {symbol}: {ex.Message}");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var gmtOffset = result.GetProperty("meta").TryGetProperty("gmtoffset", out var off) ? off.GetInt64() : 0;

            if (!result.TryGetProperty("timestamp", out var timestamps))
                return []; // no trading days in range

            var closes = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");

            var quotes = new List<(DateOnly, decimal)>();
            for (var i = 0; i < timestamps.GetArrayLength(); i++)
            {
                var closeElement = closes[i];
                if (closeElement.ValueKind == JsonValueKind.Null) continue;

                // Attribute the bar to the trading day in the exchange's local time zone.
                var localSeconds = timestamps[i].GetInt64() + gmtOffset;
                var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(localSeconds).UtcDateTime);
                quotes.Add((date, Math.Round(closeElement.GetDecimal(), 4)));
            }

            // Yahoo sometimes emits two bars for the latest day; keep the last one per date.
            return quotes
                .GroupBy(q => q.Item1)
                .Select(g => g.Last())
                .OrderBy(q => q.Item1)
                .ToList();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or IndexOutOfRangeException or InvalidOperationException)
        {
            logger.LogError(ex, "Yahoo chart response for {Symbol} could not be parsed", symbol);
            throw new CopperDomainException(failureCode, $"Unexpected response from price source for {symbol}.");
        }
    }
}
