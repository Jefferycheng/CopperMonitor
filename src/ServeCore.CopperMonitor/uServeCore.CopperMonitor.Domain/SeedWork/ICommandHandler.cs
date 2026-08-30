using MediatR;

namespace uServeCore.CopperMonitor.Domain.SeedWork;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}
