using MediatR;

namespace uServeCore.CopperMonitor.Domain.SeedWork;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
