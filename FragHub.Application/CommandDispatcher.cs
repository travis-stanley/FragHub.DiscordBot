using FragHub.Application.Abstractions;
using ICommand = FragHub.Application.Abstractions.ICommand;

namespace FragHub.Application;

public class CommandDispatcher(IServiceProvider serviceProvider)
{
    public async Task DispatchAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        var handler = serviceProvider.GetService(typeof(ICommandHandler<TCommand>)) as ICommandHandler<TCommand> ?? 
            throw new InvalidOperationException($"Handler not registered for {typeof(TCommand).Name}");

        await handler.HandleAsync(command).ConfigureAwait(false);
    }
}

