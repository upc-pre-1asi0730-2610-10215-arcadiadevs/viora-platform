using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using Cortex.Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Application.Internal.CommandServices;

/// <summary>
/// SURV-002 tests for <see cref="AlertCommandService"/>:
/// verifies that <c>AlertGeneratedIntegrationEvent</c> is published on the
/// in-process bus when an alert is created with <c>ThreatType ==
/// PHENOLOGICAL_RISK</c>, and is NOT published for any other threat type.
/// </summary>
public class AlertCommandServiceCrossBcEventTests
{
    private const long PlotIdValue = 42L;
    private const string Title = "Test Alert";
    private const string RiskExplanation = "Some risk";

    private readonly IAlertRepository _alertRepository = Substitute.For<IAlertRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private AlertCommandService CreateSut() => new(_alertRepository, _unitOfWork, _mediator);

    private static CreateAlertCommand NewCommand(string alertType) => new(
        PlotId: PlotIdValue,
        AlertType: alertType,
        Severity: EAlertSeverity.MEDIUM.ToString(),
        Title: Title,
        RiskExplanation: RiskExplanation,
        Sources: new List<string>(),
        DataProviders: new List<string>(),
        SupportingData: new Dictionary<string, string>()
    );

    [Fact]
    public async Task Handle_CreateAlert_WithPhenologicalRisk_PublishesAlertGeneratedIntegrationEvent()
    {
        // GIVEN an alert command for PHENOLOGICAL_RISK
        var command = NewCommand(EThreatType.PHENOLOGICAL_RISK.ToString());

        // WHEN the command is handled
        var sut = CreateSut();
        var result = await sut.Handle(command, CancellationToken.None);

        // THEN the alert is created successfully
        Assert.True(result.IsSuccess);
        var alert = ((Result<Alert, Error>.Success)result).Value;

        // AND an AlertGeneratedIntegrationEvent was published on the bus
        // with the alert's primitive ids (CC-1 transport)
        _ = _mediator.Received(1).PublishAsync(
            Arg.Is<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events.AlertGeneratedIntegrationEvent>(e =>
                e.AlertId == alert.Id
                && e.PlotId == PlotIdValue
                && e.ThreatType == EThreatType.PHENOLOGICAL_RISK.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("PEST_SYMPTOM")]
    [InlineData("CLIMATE_EXTREME")]
    [InlineData("CHILL_DEFICIT")]
    [InlineData("WATER_STRESS")]
    [InlineData("UNKNOWN")]
    public async Task Handle_CreateAlert_WithOtherThreatType_DoesNotPublishAlertGeneratedIntegrationEvent(string otherThreatType)
    {
        // GIVEN an alert command for a non-PHENOLOGICAL_RISK threat type
        var command = NewCommand(otherThreatType);

        // WHEN the command is handled
        var sut = CreateSut();
        var result = await sut.Handle(command, CancellationToken.None);

        // THEN the alert is still created
        Assert.True(result.IsSuccess);

        // AND no AlertGeneratedIntegrationEvent is published
        _ = _mediator.DidNotReceive().PublishAsync(
            Arg.Any<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events.AlertGeneratedIntegrationEvent>(),
            Arg.Any<CancellationToken>());
    }
}
