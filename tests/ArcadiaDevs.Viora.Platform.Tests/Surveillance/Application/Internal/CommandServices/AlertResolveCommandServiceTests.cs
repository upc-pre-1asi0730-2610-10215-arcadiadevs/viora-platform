using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using Cortex.Mediator;
using Unit = ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Unit;

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Application.Internal.CommandServices;

/// <summary>
///     WU4 tests for <see cref="AlertCommandService"/> handling
///     <see cref="ResolveAlertCommand"/>. Verifies the happy path
///     (alert found and resolved), the not-found path, and the
///     alert-not-found mapping to <see cref="SurveillanceErrors.NotFound"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class AlertResolveCommandServiceTests
{
    private const long PlotIdValue = 42L;
    private const string Title = "Test Alert";
    private const string RiskExplanation = "Some risk";

    private readonly IAlertRepository _alertRepository = Substitute.For<IAlertRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private AlertCommandService CreateSut() => new(_alertRepository, _unitOfWork, _mediator);

    private static CreateAlertCommand NewCommand() => new(
        PlotId: PlotIdValue,
        AlertType: EThreatType.PHENOLOGICAL_RISK.ToString(),
        Severity: EAlertSeverity.MEDIUM.ToString(),
        Title: Title,
        RiskExplanation: RiskExplanation,
        Sources: new List<string>(),
        DataProviders: new List<string>(),
        SupportingData: new Dictionary<string, string>()
    );

    [Fact]
    public async Task Handle_ResolveAlert_ValidInput_ReturnsSuccess()
    {
        // GIVEN an alert that exists in the repository
        var alert = new Alert(NewCommand());
        _alertRepository.FindByIdAsync((int)alert.Id, Arg.Any<CancellationToken>())
                         .Returns(alert);

        // WHEN the resolve command is handled
        var sut = CreateSut();
        var result = await sut.Handle(new ResolveAlertCommand(alert.Id), CancellationToken.None);

        // THEN the transition succeeds
        Assert.True(result.IsSuccess);
        Assert.Equal("RESOLVED", alert.Status);

        // AND the alert is persisted
        _alertRepository.Received(1).Update(alert);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResolveAlert_AlertNotFound_ReturnsFailure()
    {
        // GIVEN a non-existent alert id
        const long missingId = 999L;
        _alertRepository.FindByIdAsync((int)missingId, Arg.Any<CancellationToken>())
                         .Returns((Alert?)null);

        // WHEN the resolve command is handled
        var sut = CreateSut();
        var result = await sut.Handle(new ResolveAlertCommand(missingId), CancellationToken.None);

        // THEN it returns NotFound
        Assert.True(result.IsFailure);
        var error = ((Result<Unit, Error>.Failure)result).Error;
        Assert.Equal(SurveillanceErrors.NotFound.Code, error.Code);

        // AND no update is attempted
        _alertRepository.DidNotReceive().Update(Arg.Any<Alert>());
    }
}
