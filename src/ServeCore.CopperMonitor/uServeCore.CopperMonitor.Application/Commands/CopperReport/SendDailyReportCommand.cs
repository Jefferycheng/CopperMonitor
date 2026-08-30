using uServeCore.CopperMonitor.Application.Abstractions;
using uServeCore.CopperMonitor.Application.DTO.CopperPrice;
using uServeCore.CopperMonitor.Application.ExternalService;
using uServeCore.CopperMonitor.Application.Services;
using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Application.Commands.CopperReport;

/// <summary>Builds today's report and pushes it to the configured LINE group.</summary>
public class SendDailyReportCommand : ICommand<ResponseResult<CopperPriceReportDto>>
{
    /// <summary>Report as-of date. Null → today (UTC).</summary>
    public DateOnly? Date { get; set; }
}

public class SendDailyReportCommandHandler(
    CopperReportService reportService,
    ILineMessenger lineMessenger)
    : ICommandHandler<SendDailyReportCommand, ResponseResult<CopperPriceReportDto>>
{
    public async Task<ResponseResult<CopperPriceReportDto>> Handle(SendDailyReportCommand request, CancellationToken ct)
    {
        var asOf = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await reportService.BuildReportAsync(asOf, ct);
        await lineMessenger.PushTextAsync(report.ReportText, ct);
        return ResponseResult<CopperPriceReportDto>.Success(report, "Report sent to LINE.");
    }
}
