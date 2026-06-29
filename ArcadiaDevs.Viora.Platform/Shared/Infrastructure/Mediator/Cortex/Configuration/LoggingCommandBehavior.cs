using System.Diagnostics;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Mediator.Cortex.Configuration;

/// <summary>
///     Pipeline behavior that logs command execution with timing and status.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The command result type.</typeparam>
public class LoggingCommandBehavior<TCommand, TResult>(
    ILogger<LoggingCommandBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <inheritdoc />
    public async Task<TResult> Handle(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await next();
            stopwatch.Stop();

            logger.LogInformation(
                "{CommandName} completed in {ElapsedMs}ms (Success=true)",
                commandName,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            logger.LogError(ex,
                "{CommandName} failed after {ElapsedMs}ms",
                commandName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}