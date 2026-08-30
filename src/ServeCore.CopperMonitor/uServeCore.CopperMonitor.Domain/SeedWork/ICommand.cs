using MediatR;

namespace uServeCore.CopperMonitor.Domain.SeedWork;

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
