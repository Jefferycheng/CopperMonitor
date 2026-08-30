using System.Globalization;
using System.Text.RegularExpressions;
using MediatR;
using uServeCore.CopperMonitor.Application.DTO.CopperPrice;
using uServeCore.CopperMonitor.Application.Queries.CopperPrice;
using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Application.Services;

/// <summary>
/// Turns a chat message into a copper report reply.
///   "today" / "報告"            → today's full report
///   "2026-08-27"                → that day's price
///   "2026-08-01 2026-08-28"     → range summary (also accepts ~ or 到 as separator)
/// Returns null for anything else so normal group chatter is ignored.
/// </summary>
public partial class ChatCommandService(ISender sender)
{
    [GeneratedRegex(@"\d{4}[-/]\d{1,2}[-/]\d{1,2}")]
    private static partial Regex DateTokenRegex();

    private static readonly string[] TodayKeywords = ["today", "今天", "report", "報告", "銅價"];

    public async Task<string?> TryHandleAsync(string text, CancellationToken ct = default)
    {
        var trimmed = text.Trim();

        if (TodayKeywords.Any(k => string.Equals(trimmed, k, StringComparison.OrdinalIgnoreCase)))
        {
            var report = await sender.Send(new GetCopperPriceReportQuery(), ct);
            return report.Data?.ReportText;
        }

        var dates = DateTokenRegex().Matches(trimmed)
            .Select(m => DateOnly.TryParse(m.Value.Replace('/', '-'), CultureInfo.InvariantCulture, out var d) ? d : (DateOnly?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        return dates.Count switch
        {
            1 => await HistoryReplyAsync(dates[0], dates[0], ct),
            2 => await HistoryReplyAsync(dates.Min(), dates.Max(), ct),
            _ => null
        };
    }

    private async Task<string> HistoryReplyAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        List<CopperPriceDayDto> days;
        try
        {
            var result = await sender.Send(new GetCopperPriceHistoryQuery { From = from, To = to }, ct);
            days = result.Data ?? [];
        }
        catch (CopperDomainException ex)
        {
            return $"查詢失敗：{ex.Message}";
        }

        if (days.Count == 0)
            return $"{from:yyyy-MM-dd} ~ {to:yyyy-MM-dd} 沒有交易資料。";

        // LINE text messages cap at 5000 chars — list days for short ranges, summarize long ones.
        if (days.Count <= 31)
        {
            var lines = days.Select(d =>
                $"{d.Date}：{d.CloseUsdPerLb.ToString("0.####", CultureInfo.InvariantCulture)} USD/lb" +
                (d.TwdPerTon.HasValue ? $"｜{d.TwdPerTon.Value.ToString("N0", CultureInfo.InvariantCulture)} TWD/噸" : ""));
            return $"📈 銅價 {from:yyyy-MM-dd} ~ {to:yyyy-MM-dd}\n" + string.Join("\n", lines);
        }

        var first = days[0];
        var last = days[^1];
        var change = first.CloseUsdPerLb == 0
            ? 0
            : Math.Round((last.CloseUsdPerLb - first.CloseUsdPerLb) / first.CloseUsdPerLb * 100m, 2);
        return $"📈 銅價 {from:yyyy-MM-dd} ~ {to:yyyy-MM-dd}（{days.Count} 個交易日）\n" +
               $"起：{first.Date}：{first.CloseUsdPerLb.ToString("0.####", CultureInfo.InvariantCulture)} USD/lb\n" +
               $"迄：{last.Date}：{last.CloseUsdPerLb.ToString("0.####", CultureInfo.InvariantCulture)} USD/lb\n" +
               $"最高：{days.Max(d => d.CloseUsdPerLb).ToString("0.####", CultureInfo.InvariantCulture)}｜最低：{days.Min(d => d.CloseUsdPerLb).ToString("0.####", CultureInfo.InvariantCulture)}\n" +
               $"區間漲跌：{(change >= 0 ? "+" : "")}{change}%";
    }
}
