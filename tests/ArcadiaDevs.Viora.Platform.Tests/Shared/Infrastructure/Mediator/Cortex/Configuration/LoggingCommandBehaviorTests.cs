using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Mediator.Cortex.Configuration;
using Cortex.Mediator.Commands;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Infrastructure.Mediator.Cortex.Configuration;

public class LoggingCommandBehaviorTests
{
    private readonly ILogger<LoggingCommandBehavior<TestCommand, TestResult>> _logger =
        Substitute.For<ILogger<LoggingCommandBehavior<TestCommand, TestResult>>>();

    private readonly LoggingCommandBehavior<TestCommand, TestResult> _sut;

    public LoggingCommandBehaviorTests()
    {
        _sut = new LoggingCommandBehavior<TestCommand, TestResult>(_logger);
    }

    [Fact]
    public async Task Handle_OnSuccess_LogsInformationWithCommandNameAndElapsedMs()
    {
        // Arrange
        var command = new TestCommand("test-value");
        var expectedResult = new TestResult("result-value");
        CommandHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _sut.Handle(command, next, CancellationToken.None);

        // Assert — result passes through unchanged
        Assert.Equal(expectedResult, result);

        // Assert — Information log was emitted
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("TestCommand")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_OnSuccess_LogsElapsedMilliseconds()
    {
        // Arrange
        var command = new TestCommand("test-value");
        var expectedResult = new TestResult("result-value");
        CommandHandlerDelegate<TestResult> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _sut.Handle(command, next, CancellationToken.None);

        // Assert — log message contains "ms" for elapsed time
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ms")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>());
    }

    // Test types for the behavior
    public record TestCommand(string Value) : ICommand<TestResult>;
    public record TestResult(string Value);
}
