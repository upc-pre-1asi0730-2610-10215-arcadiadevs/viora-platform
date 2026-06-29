using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.EventHandlers;

public class NutritionApplicationCertifiedEventHandlerTests
{
    private readonly ILogger<NutritionApplicationCertifiedEventHandler> _logger =
        Substitute.For<ILogger<NutritionApplicationCertifiedEventHandler>>();

    private readonly NutritionApplicationCertifiedEventHandler _sut;

    public NutritionApplicationCertifiedEventHandlerTests()
    {
        _sut = new NutritionApplicationCertifiedEventHandler(_logger);
    }

    [Fact]
    public async Task Handle_LogsInformationAboutReceivedEvent()
    {
        // Arrange
        var domainEvent = new NutritionApplicationCertifiedEvent(
            PlanId: 1,
            PlotId: 100,
            UserId: 42,
            ApplicationDate: new DateOnly(2026, 6, 29));

        // Act
        await _sut.Handle(domainEvent, CancellationToken.None);

        // Assert — exactly one Information log was emitted
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("NutritionApplicationCertifiedEvent")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object?, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_ReturnsCompletedTask()
    {
        // Arrange
        var domainEvent = new NutritionApplicationCertifiedEvent(
            PlanId: 1,
            PlotId: 100,
            UserId: 42,
            ApplicationDate: new DateOnly(2026, 6, 29));

        // Act
        var result = _sut.Handle(domainEvent, CancellationToken.None);

        // Assert — returns completed task (no async side effects)
        Assert.True(result.IsCompleted);
    }
}
