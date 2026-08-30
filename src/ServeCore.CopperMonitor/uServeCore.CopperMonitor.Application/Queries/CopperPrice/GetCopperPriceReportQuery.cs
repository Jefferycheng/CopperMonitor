using uServeCore.CopperMonitor.Application.Abstractions;
using uServeCore.CopperMonitor.Application.DTO.CopperPrice;
using uServeCore.CopperMonitor.Application.Services;
using uServeCore.CopperMonitor.Domain.SeedWork;

namespace uServeCore.CopperMonitor.Application.Queries.CopperPrice;

public class GetCopperPriceReportQuery : IQuery<ResponseResult<CopperPriceReportDto>>
{
    /// <summary>Report as-of date (yyyy-MM-dd). Null → today (UTC).</summary>
    public DateOnly? Date { get; set; }
}

public class GetCopperPriceReportQueryHandler(CopperReportService reportService)
    : IQueryHandler<GetCopperPriceReportQuery, ResponseResult<CopperPriceReportDto>>
{
    public async Task<ResponseResult<CopperPriceReportDto>> Handle(GetCopperPriceReportQuery request, CancellationToken ct)
    {
        var asOf = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await reportService.BuildReportAsync(asOf, ct);
        return ResponseResult<CopperPriceReportDto>.Success(report);
    }
}
