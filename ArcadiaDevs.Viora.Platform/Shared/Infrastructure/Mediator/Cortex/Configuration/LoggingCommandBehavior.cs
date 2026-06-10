using Cortex.Mediator.Commands;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Mediator.Cortex.Configuration;

/// <summary>
///     Pipeline behavior that logs command execution.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The command result type.</typeparam>
public class LoggingCommandBehavior<TCommand, TResult>
    : ICommandPipelineBehavior<TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <inheritdoc />
    public async Task<TResult> Handle(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        // Log before/after
        return await next();
    }
}