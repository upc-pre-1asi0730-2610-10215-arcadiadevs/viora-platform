using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using Cortex.Mediator;

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Application.Internal.CommandServices;

/// <summary>
///     WU5 tests for <see cref="PestSightingCommandService"/>.
///     Verifies the create and review happy paths using NSubstitute
///     for all interface dependencies.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class PestSightingCommandServiceTests
{
    private const long PlotIdValue = 42L;
    private const long ReporterUserIdValue = 100L;
    private const int ReportIdValue = 7;

    private readonly IPestSightingReportRepository _reportRepository = Substitute.For<IPestSightingReportRepository>();
    private readonly IExternalAgronomicService _externalAgronomicService = Substitute.For<IExternalAgronomicService>();
    private readonly IAlertCommandService _alertCommandService = Substitute.For<IAlertCommandService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ThreatInferenceService _threatInferenceService = new();

    private PestSightingCommandService CreateSut() => new(
        _reportRepository,
        _externalAgronomicService,
        _threatInferenceService,
        _alertCommandService,
        _unitOfWork,
        _mediator);

    private static CreatePestSightingReportCommand NewCreateCommand(
        string severity = "LOW",
        List<string>? symptoms = null) => new(
        PlotId: PlotIdValue,
        ReporterUserId: ReporterUserIdValue,
        RiskZone: "FULL_PLOT",
        Symptoms: symptoms ?? new List<string> { "yellowing leaves" },
        ObservedSeverity: severity,
        Notes: "Test notes");

    /// <summary>
    ///     Creates a <see cref="PestSightingReport"/> with a given Id
    ///     via reflection (EF Core normally sets this at materialisation).
    /// </summary>
    private static PestSightingReport CreateReportWithId(int id, long reporterUserId, string severity = "LOW")
    {
        var report = new PestSightingReport(NewCreateCommand(severity: severity));
        SetBackingField(report, "Id", id);
        SetBackingField(report, "ReporterUserId", new ReporterUserId(reporterUserId));
        return report;
    }

    private static void SetBackingField(object target, string propertyName, object value)
    {
        var backingField = target.GetType().GetField(
            $"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(backingField);
        backingField!.SetValue(target, value);
    }

    [Fact]
    public async Task Handle_ReportPestSighting_ValidInput_ReturnsSuccess()
    {
        // GIVEN valid create command and external service responses
        var command = NewCreateCommand(severity: "LOW");
        _externalAgronomicService.FetchCurrentNdviByPlotIdAsync(
                command.PlotId, command.ReporterUserId, Arg.Any<CancellationToken>())
            .Returns(0.65);

        // WHEN the create command is handled
        var sut = CreateSut();
        var result = await sut.Handle(command, CancellationToken.None);

        // THEN the result is a success
        Assert.True(result.IsSuccess);
        var aggregate = ((Result<PestSightingReport, Error>.Success)result).Value;
        Assert.Equal(PlotIdValue, aggregate.PlotId.Value);
        Assert.Equal(ReporterUserIdValue, aggregate.ReporterUserId.Value);
        Assert.True(aggregate.Evaluated);

        // AND the report is persisted
        await _reportRepository.Received(1).AddAsync(
            Arg.Any<PestSightingReport>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());

        // AND the domain event is published
        await _mediator.Received(1).PublishAsync(
            Arg.Any<PestSightingReportEvaluatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReviewPestSightingReport_ValidInput_ReturnsSuccess()
    {
        // GIVEN a report in UNDER_REVIEW status owned by the reporter
        var report = CreateReportWithId(ReportIdValue, ReporterUserIdValue);
        _reportRepository.FindByIdAsync(ReportIdValue, Arg.Any<CancellationToken>())
            .Returns(report);

        _alertCommandService.Handle(
                Arg.Any<ConfirmAlertFromInspectionCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result<long, Error>.Success(42L)); // linked alert exists

        // WHEN the review command is handled with CONFIRMED outcome
        var command = new ReviewPestSightingReportCommand(
            ReportId: ReportIdValue,
            ReporterUserId: ReporterUserIdValue,
            Outcome: "CONFIRMED");

        var sut = CreateSut();
        var result = await sut.Handle(command, CancellationToken.None);

        // THEN the result is a success
        Assert.True(result.IsSuccess);
        var reviewed = ((Result<PestSightingReport, Error>.Success)result).Value;
        Assert.Equal(EReportStatus.CONFIRMED, reviewed.Status);
        Assert.True(reviewed.AlertConfirmed);

        // AND the report is updated
        _reportRepository.Received(1).Update(report);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());

        // AND the alert command service is called to mirror onto alert
        await _alertCommandService.Received(1).Handle(
            Arg.Is<ConfirmAlertFromInspectionCommand>(c => c.ReportId == ReportIdValue),
            Arg.Any<CancellationToken>());
    }
}
