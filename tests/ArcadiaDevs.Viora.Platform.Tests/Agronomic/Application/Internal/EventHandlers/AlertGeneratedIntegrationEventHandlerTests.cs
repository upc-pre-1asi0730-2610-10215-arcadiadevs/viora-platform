using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.EventHandlers;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Application.Internal.EventHandlers;

/// <summary>
/// SURV-002 tests for <see cref="AlertGeneratedIntegrationEventHandler"/>:
/// the Agronomic-side handler must filter on <c>PHENOLOGICAL_RISK</c>,
/// wrap the primitive <c>PlotId</c> in the BC-local
/// <c>Agronomic.Domain.Model.ValueObjects.PlotId</c> (CC-1), and call
/// <c>IRecommendDynamicNutritionPlanCommandService</c>.
/// </summary>
public class AlertGeneratedIntegrationEventHandlerTests
{
    private readonly IRecommendDynamicNutritionPlanCommandService _recommendService =
        Substitute.For<IRecommendDynamicNutritionPlanCommandService>();

    private readonly ILogger<AlertGeneratedIntegrationEventHandler> _logger =
        Substitute.For<ILogger<AlertGeneratedIntegrationEventHandler>>();

    private AlertGeneratedIntegrationEventHandler CreateSut() =>
        new(_recommendService, _logger);

    [Fact]
    public async Task Handle_WithPhenologicalRisk_RecommendsDynamicNutritionPlanForWrappedPlotId()
    {
        // GIVEN an AlertGeneratedIntegrationEvent with ThreatType=PHENOLOGICAL_RISK
        const long plotId = 42L;
        const long alertId = 7L;
        var @event = new ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events.AlertGeneratedIntegrationEvent(
            PlotId: plotId,
            AlertId: alertId,
            ThreatType: EThreatType.PHENOLOGICAL_RISK.ToString(),
            GeneratedAt: DateTime.UtcNow);

        // WHEN the handler runs
        var sut = CreateSut();
        await sut.Handle(@event, CancellationToken.None);

        // THEN IRecommendDynamicNutritionPlanCommandService is called once
        // with a freshly-wrapped Agronomic.PlotId matching the primitive
        // plot id from the event (CC-1 wrap)
        _ = _recommendService.Received(1).Handle(
            Arg.Is<RecommendDynamicNutritionCommand>(c =>
                c.PlotId == (int)plotId
                && c.UserId == alertId),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("PEST_SYMPTOM")]
    [InlineData("CLIMATE_EXTREME")]
    [InlineData("WATER_STRESS")]
    [InlineData("UNKNOWN")]
    public async Task Handle_WithOtherThreatType_DoesNotCallRecommendService(string otherThreatType)
    {
        // GIVEN an event whose ThreatType is anything other than PHENOLOGICAL_RISK
        var @event = new ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events.AlertGeneratedIntegrationEvent(
            PlotId: 42L,
            AlertId: 7L,
            ThreatType: otherThreatType,
            GeneratedAt: DateTime.UtcNow);

        // WHEN the handler runs
        var sut = CreateSut();
        await sut.Handle(@event, CancellationToken.None);

        // THEN the recommend service is never called
        _ = _recommendService.DidNotReceive().Handle(
            Arg.Any<RecommendDynamicNutritionCommand>(),
            Arg.Any<CancellationToken>());
    }
}
