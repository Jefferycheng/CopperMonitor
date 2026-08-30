using MediatR;

namespace CopperMonitor.Domain.SeedWork;

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
