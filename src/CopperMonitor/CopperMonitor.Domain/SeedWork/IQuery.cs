using MediatR;

namespace CopperMonitor.Domain.SeedWork;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
