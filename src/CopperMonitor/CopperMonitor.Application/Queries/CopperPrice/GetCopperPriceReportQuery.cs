using CopperMonitor.Application.Abstractions;
using CopperMonitor.Application.DTO.CopperPrice;
using CopperMonitor.Application.Services;
using CopperMonitor.Domain.SeedWork;

namespace CopperMonitor.Application.Queries.CopperPrice;

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
