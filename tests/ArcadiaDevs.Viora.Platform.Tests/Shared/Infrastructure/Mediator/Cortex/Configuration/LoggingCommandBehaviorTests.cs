using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Mediator.Cortex.Configuration;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Infrastructure.Mediator.Cortex.Configuration;

/// <summary>
///     Unit tests for <see cref="LoggingCommandBehavior{TCommand, TResult}"/>
///     (SHARED-006) — the Cortex.Mediator pipeline behavior that
///     logs command execution with timing and status.
///     <para>
///         The tests cover 4 scenarios from the Phase 2 A6 spec
///         (S1.8 + S1.9 in obs #75) and the design §1.10 F1
///         commit "LoggingCommandBehavior structured log fields"
///         expansion:
///     </para>
///     <list type="number">
///         <item>
///             S1.9 + F1 expansion — OnSuccess: a single
///             <c>LogLevel.Information</c> call is emitted with
///             the command type name in the structured payload.
///         </item>
///         <item>
///             S1.9 + F1 expansion — OnSuccess: the structured
///             payload contains the elapsed-ms field ("ms" suffix
///             in the ToString() output).
///         </item>
///         <item>
///             S1.8 — OnFailure: when the inner handler throws, the
///             exception propagates to the caller AND the logger
///             receives a <c>LogLevel.Error</c> call (>= Warning)
///             with the command type name AND the elapsed-ms
///             field in the structured payload AND the original
///             exception is passed as the <c>Exception</c>
///             argument.
///         </item>
///         <item>
///             F1 expansion — Result passes through unchanged on
///             success (the behavior must not transform the inner
///             handler's return value).
///         </item>
///     </list>
///     <para>
///         The behavior uses <see cref="System.Diagnostics.Stopwatch"/>
///         for timing (not <see cref="ArcadiaDevs.Viora.Platform.Shared.Domain.IClock"/>),
///         so the test does not inject a <c>FakeClock</c> — the
///         elapsed-ms field is a non-deterministic wall-clock
///         measurement, asserted only for the presence of the
///         "ms" suffix in the rendered payload (the value is
///         always > 0 ms after a real handler invocation).
///     </para>
/// </summary>
[Trait("Category", "Unit")]
public class LoggingCommandBehaviorTests
{
    private readonly ILogger<LoggingCommandBehavior<TestCommand, TestResult>> _logger =
        Substitute.For<ILogger<LoggingCommandBehavior<TestCommand, TestResult>>>();

    private readonly LoggingCommandBehavior<TestCommand, TestResult> _sut;

    public LoggingCommandBehaviorTests()
    {
        _sut = new LoggingCommandBehavior<TestCommand, TestResult>(_logger);
    }

    /// <summary>
    ///     S1.9 (Phase 2 S6.10a) — On a successful handler
    ///     invocation, the logger receives exactly 1
    ///     <c>LogLevel.Information</c> call whose structured
    ///     payload contains the command's type name.
    /// </summary>
    [Fact]
    public async Task Handle_OnSuccess_LogsInformationWithCommandNameAndElapsedMs()
    {
        // GIVEN a behavior + a command + a successful inner
        // handler that returns the expected result.
        var command = new TestCommand("test-value");
        var expectedResult = new TestResult("result-value");
        CommandHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // WHEN the behavior processes the command.
        var result = await _sut.Handle(command, next, CancellationToken.None);

        // THEN the result passes through unchanged AND the
        // logger receives exactly 1 Information call with
        // the command type name in the structured payload.
        Assert.Equal(expectedResult, result);
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("TestCommand")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>());
    }

    /// <summary>
    ///     S1.9 (Phase 2 S6.10b) — On a successful handler
    ///     invocation, the structured payload contains the
    ///     elapsed-ms field ("ms" suffix in the rendered output).
    /// </summary>
    [Fact]
    public async Task Handle_OnSuccess_LogsElapsedMilliseconds()
    {
        // GIVEN a behavior + a command + a successful inner
        // handler.
        var command = new TestCommand("test-value");
        var expectedResult = new TestResult("result-value");
        CommandHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // WHEN the behavior processes the command.
        var result = await _sut.Handle(command, next, CancellationToken.None);

        // THEN the result passes through AND the logger
        // receives exactly 1 Information call with the "ms"
        // suffix in the structured payload (the elapsed-ms
        // field is always present on success).
        Assert.Equal(expectedResult, result);
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ms")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>());
    }

    /// <summary>
    ///     S1.8 (Phase 2 S6.9) — On a handler exception, the
    ///     exception propagates to the caller AND the logger
    ///     receives a <c>LogLevel.Error</c> call (Error &gt;=
    ///     Warning) with the command type name AND the
    ///     elapsed-ms field in the structured payload AND the
    ///     original exception is passed as the <c>Exception</c>
    ///     argument.
    /// </summary>
    [Fact]
    public async Task Handle_OnFailure_LogsErrorAndPropagates()
    {
        // GIVEN a behavior + a command + an inner handler
        // that throws a known exception.
        var command = new TestCommand("test-value");
        var thrownException = new InvalidOperationException("simulated handler failure");
        CommandHandlerDelegate<TestResult> next = () => throw thrownException;

        // WHEN the behavior processes the command.
        // THEN the inner exception propagates to the caller.
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(command, next, CancellationToken.None));
        Assert.Same(thrownException, actualException);

        // AND the logger receives exactly 1 Error call (Error
        // >= Warning per the spec) with the command type
        // name, the elapsed-ms field, and the original
        // exception as the exception argument.
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("TestCommand")
                              && o.ToString()!.Contains("ms")),
            thrownException,
            Arg.Any<Func<object?, Exception?, string>>());
    }

    /// <summary>
    ///     F1 expansion — The behavior does not transform the
    ///     inner handler's return value. The result returned to
    ///     the caller is the same instance returned by the inner
    ///     handler (reference equality for reference types, value
    ///     equality for value types).
    /// </summary>
    [Fact]
    public async Task Handle_PassesResultThroughUnchanged()
    {
        // GIVEN a behavior + a command + an inner handler
        // that returns a specific result instance.
        var command = new TestCommand("test-value");
        var expectedResult = new TestResult("result-value");
        CommandHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // WHEN the behavior processes the command.
        var result = await _sut.Handle(command, next, CancellationToken.None);

        // THEN the returned result is the SAME instance as
        // the inner handler's return value (no transformation,
        // no wrapping, no unwrapping).
        Assert.Same(expectedResult, result);
    }

    // Test types for the behavior
    public record TestCommand(string Value) : ICommand<TestResult>;
    public record TestResult(string Value);
}
