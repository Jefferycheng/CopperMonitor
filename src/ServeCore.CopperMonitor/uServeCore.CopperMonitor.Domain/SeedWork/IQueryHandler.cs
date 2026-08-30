using MediatR;

namespace uServeCore.CopperMonitor.Domain.SeedWork;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}
