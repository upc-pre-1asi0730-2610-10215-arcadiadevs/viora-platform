using System.Net.Mime;
using System.Reflection;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Unit = ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Unit;

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Interfaces.Rest.Controllers;

/// <summary>
/// SURV-003 tests for the <see cref="AlertsController"/> state-transition
/// behavior, folded into <c>PATCH /api/v1/alerts/{id}</c> (confirm, dismiss,
/// escalate) and <c>PUT /api/v1/alerts/{id}/report/{reportId}</c>
/// (link-report), plus the sort-placeholder fix. Each transition maps
/// <see cref="Result{TValue, TError}"/> failures to RFC 7807
/// <see cref="ProblemDetails"/> (CC-6) and returns 200 on success.
/// </summary>
public class AlertsControllerStateTransitionTests
{
    private readonly IAlertQueryService _alertQueryService = Substitute.For<IAlertQueryService>();
    private readonly IAlertCommandService _alertCommandService = Substitute.For<IAlertCommandService>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer = StubLocalizer();
    private readonly ProblemDetailsFactory _problemDetailsFactory = new TestProblemDetailsFactory();

    private static IStringLocalizer<ErrorMessages> StubLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<ErrorMessages>>();
        localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), value: call.ArgAt<string>(0)));
        return localizer;
    }

    /// <summary>
    /// Minimal concrete <see cref="ProblemDetailsFactory"/> for unit tests.
    /// Returns a non-null <see cref="ProblemDetails"/> for every call so the
    /// controller's <c>CreateProblemDetails(...)</c> call never NREs.
    /// </summary>
    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new()
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };
    }

    private AlertsController CreateController()
    {
        var controller = new AlertsController(
            _alertQueryService,
            _alertCommandService,
            _errorLocalizer,
            _problemDetailsFactory);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
        return controller;
    }

    [Fact]
    public async Task Patch_ConfirmAction_UnderReviewWithRaiseSeverity_Returns200()
    {
        // GIVEN a command service that returns a successful confirm
        _alertCommandService.Handle(Arg.Any<ConfirmAlertCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Success(Unit.Value));

        // AND a query service that returns the just-confirmed alert
        _alertQueryService.Handle(Arg.Any<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries.GetAlertByIdQuery>(),
                                  Arg.Any<CancellationToken>())
                          .Returns(BuildAlert(id: 42L, status: "UNDER_REVIEW", severity: EAlertSeverity.MEDIUM));

        var controller = CreateController();

        // WHEN the controller PATCHes the alert with status UNDER_REVIEW and raiseSeverity=true
        var resource = new UpdateAlertResource(Status: "UNDER_REVIEW", RaiseSeverity: true);
        var result = await controller.UpdateAlert(42L, resource, CancellationToken.None);

        // THEN the result is 200 OK with the updated alert resource
        var ok = Assert.IsType<OkObjectResult>(result);
        var alertResource = Assert.IsType<AlertResource>(ok.Value);
        Assert.Equal(42L, alertResource.Id);
        Assert.Equal("UNDER_REVIEW", alertResource.Status);

        // AND the command service received a ConfirmAlertCommand (not MarkAlertAsReviewedCommand)
        await _alertCommandService.Received(1).Handle(
            Arg.Is<ConfirmAlertCommand>(c => c.AlertId == 42L),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Patch_ConfirmAction_OnDismissed_Returns400ProblemDetails()
    {
        // GIVEN a command service that returns a failure (ALERT_TERMINAL)
        _alertCommandService.Handle(Arg.Any<ConfirmAlertCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Failure(
                               new Error("ALERT_TERMINAL", "Cannot confirm a terminal alert.")));

        var controller = CreateController();

        // WHEN the controller PATCHes the alert with status UNDER_REVIEW and raiseSeverity=true
        var resource = new UpdateAlertResource(Status: "UNDER_REVIEW", RaiseSeverity: true);
        var result = await controller.UpdateAlert(42L, resource, CancellationToken.None);

        // THEN the result is 400 with a ProblemDetails body (CC-6);
        // the state machine's ALERT_TERMINAL failure is mapped to 400.
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    [Fact]
    public async Task Patch_EscalateAction_StatusOmittedWithRaiseSeverity_Returns200()
    {
        // GIVEN a command service that returns a successful escalate
        _alertCommandService.Handle(Arg.Any<EscalateAlertCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Success(Unit.Value));

        // AND a query service that returns the just-escalated alert (no status change)
        _alertQueryService.Handle(Arg.Any<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries.GetAlertByIdQuery>(),
                                  Arg.Any<CancellationToken>())
                          .Returns(BuildAlert(id: 43L, status: "ACTIVE", severity: EAlertSeverity.HIGH));

        var controller = CreateController();

        // WHEN the controller PATCHes the alert with status omitted and raiseSeverity=true
        var resource = new UpdateAlertResource(Status: null, RaiseSeverity: true);
        var result = await controller.UpdateAlert(43L, resource, CancellationToken.None);

        // THEN the result is 200 OK with the updated alert resource
        var ok = Assert.IsType<OkObjectResult>(result);
        var alertResource = Assert.IsType<AlertResource>(ok.Value);
        Assert.Equal(43L, alertResource.Id);
        Assert.Equal("ACTIVE", alertResource.Status);

        // AND the command service received an EscalateAlertCommand
        await _alertCommandService.Received(1).Handle(
            Arg.Is<EscalateAlertCommand>(c => c.AlertId == 43L),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Patch_EscalateAction_AlertNotFound_Returns404ProblemDetails()
    {
        // GIVEN a command service that returns NotFound
        _alertCommandService.Handle(Arg.Any<EscalateAlertCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Failure(SurveillanceErrors.NotFound));

        var controller = CreateController();

        // WHEN the controller PATCHes a non-existent alert with raiseSeverity=true
        var resource = new UpdateAlertResource(Status: null, RaiseSeverity: true);
        var result = await controller.UpdateAlert(999L, resource, CancellationToken.None);

        // THEN the result is 404 with a ProblemDetails body (CC-6)
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    [Fact]
    public async Task Patch_StatusAndRaiseSeverityBothOmitted_Returns400EmptyResource()
    {
        var controller = CreateController();

        // WHEN the controller PATCHes with nothing to do
        var resource = new UpdateAlertResource(Status: null);
        var result = await controller.UpdateAlert(1L, resource, CancellationToken.None);

        // THEN the result is 400 with the legacy EmptyResource body (no transition attempted)
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<EmptyResource>(badRequest.Value);
        await _alertCommandService.DidNotReceiveWithAnyArgs().Handle(Arg.Any<ConfirmAlertCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Put_LinkReport_Valid_Returns200()
    {
        // GIVEN a command service that returns a successful link
        _alertCommandService.Handle(Arg.Any<LinkAlertReportCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Success(Unit.Value));

        // AND a query service that returns the alert with the report linked
        _alertQueryService.Handle(Arg.Any<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries.GetAlertByIdQuery>(),
                                  Arg.Any<CancellationToken>())
                          .Returns(BuildAlert(id: 44L, status: "ACTIVE", severity: EAlertSeverity.LOW));

        var controller = CreateController();

        // WHEN the controller PUTs the linked report onto the alert
        var result = await controller.LinkReport(44L, 900L, CancellationToken.None);

        // THEN the result is 200 OK with the updated alert resource
        var ok = Assert.IsType<OkObjectResult>(result);
        var alertResource = Assert.IsType<AlertResource>(ok.Value);
        Assert.Equal(44L, alertResource.Id);

        // AND the command service received a LinkAlertReportCommand with the route reportId
        await _alertCommandService.Received(1).Handle(
            Arg.Is<LinkAlertReportCommand>(c => c.AlertId == 44L && c.ReportId == 900L),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Put_LinkReport_AlertNotFound_Returns404ProblemDetails()
    {
        // GIVEN a command service that returns NotFound
        _alertCommandService.Handle(Arg.Any<LinkAlertReportCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Failure(SurveillanceErrors.NotFound));

        var controller = CreateController();

        // WHEN the controller PUTs a linked report onto a non-existent alert
        var result = await controller.LinkReport(999L, 900L, CancellationToken.None);

        // THEN the result is 404 with a ProblemDetails body (CC-6)
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    [Fact]
    public async Task GetAlerts_SortBySeverity_NotEmptyList()
    {
        // GIVEN a query service that returns a non-empty list of summaries
        var summaries = new List<AlertSummaryResource>
        {
            NewSummary(1L, EAlertSeverity.HIGH, EThreatType.PEST_SYMPTOM, "ACTIVE"),
            NewSummary(2L, EAlertSeverity.LOW, EThreatType.PHENOLOGICAL_RISK, "ACTIVE"),
        };
        _alertQueryService.Handle(Arg.Any<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries.GetAlertsByUserIdQuery>(),
                                  Arg.Any<CancellationToken>())
                          .Returns(summaries);

        var controller = CreateController();

        // WHEN the controller handles GET /alerts?sort=severity
        var result = await controller.GetAlerts(
            userId: 100L,
            sort: "severity",
            limit: 10,
            CancellationToken.None);

        // THEN the result is 200 OK with the non-empty list
        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<AlertSummaryResource>>(ok.Value);
        Assert.NotEmpty(returned);
    }

    [Fact]
    public async Task GetAlerts_OnEmptyTimeline_ReturnsEmptyArrayNot500()
    {
        // GIVEN a query service that returns an empty list (no data)
        _alertQueryService.Handle(Arg.Any<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries.GetAlertsByUserIdQuery>(),
                                  Arg.Any<CancellationToken>())
                          .Returns(new List<AlertSummaryResource>());

        var controller = CreateController();

        // WHEN the controller handles GET /alerts?sort=type (empty timeline)
        var result = await controller.GetAlerts(
            userId: 100L,
            sort: "type",
            limit: 10,
            CancellationToken.None);

        // THEN the result is 200 OK with an empty enumerable (NOT 500)
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var returned = Assert.IsAssignableFrom<IEnumerable<AlertSummaryResource>>(ok.Value);
        Assert.Empty(returned);
    }

    // ============================================================
    // WU4: PATCH /api/v1/alerts/{id} — RESOLVED and DISMISSED actions
    // ============================================================

    [Fact]
    public async Task Patch_ResolveAction_Returns200()
    {
        // GIVEN a command service that returns a successful resolve
        _alertCommandService.Handle(Arg.Any<ResolveAlertCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Success(Unit.Value));

        // AND a query service that returns the just-resolved alert
        _alertQueryService.Handle(Arg.Any<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries.GetAlertByIdQuery>(),
                                  Arg.Any<CancellationToken>())
                          .Returns(BuildAlert(id: 55L, status: "RESOLVED", severity: EAlertSeverity.MEDIUM));

        var controller = CreateController();

        // WHEN the controller PATCHes the alert with status RESOLVED
        var resource = new UpdateAlertResource(Status: "RESOLVED");
        var result = await controller.UpdateAlert(55L, resource);

        // THEN the result is 200 OK with the resolved alert resource
        var ok = Assert.IsType<OkObjectResult>(result);
        var alertResource = Assert.IsType<AlertResource>(ok.Value);
        Assert.Equal(55L, alertResource.Id);
        Assert.Equal("RESOLVED", alertResource.Status);
    }

    [Fact]
    public async Task Patch_ResolveAction_AlertNotFound_Returns404()
    {
        // GIVEN a command service that returns NotFound
        _alertCommandService.Handle(Arg.Any<ResolveAlertCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Failure(SurveillanceErrors.NotFound));

        var controller = CreateController();

        // WHEN the controller PATCHes a non-existent alert with status RESOLVED
        var resource = new UpdateAlertResource(Status: "RESOLVED");
        var result = await controller.UpdateAlert(999L, resource);

        // THEN the result is 404
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
    }

    [Fact]
    public async Task Patch_DismissAction_WithReason_StoresReason()
    {
        // GIVEN a command service that returns a successful dismiss
        _alertCommandService.Handle(Arg.Any<DismissAlertCommand>(), Arg.Any<CancellationToken>())
                           .Returns(new Result<Unit, Error>.Success(Unit.Value));

        // AND a query service that returns the just-dismissed alert
        _alertQueryService.Handle(Arg.Any<ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries.GetAlertByIdQuery>(),
                                  Arg.Any<CancellationToken>())
                          .Returns(BuildAlert(id: 60L, status: "DISMISSED", severity: EAlertSeverity.LOW));

        var controller = CreateController();

        // WHEN the controller PATCHes the alert with status DISMISSED and a reason
        var resource = new UpdateAlertResource(Status: "DISMISSED", Reason: "False positive after field inspection.");
        var result = await controller.UpdateAlert(60L, resource);

        // THEN the result is 200 OK
        var ok = Assert.IsType<OkObjectResult>(result);
        var alertResource = Assert.IsType<AlertResource>(ok.Value);
        Assert.Equal(60L, alertResource.Id);
        Assert.Equal("DISMISSED", alertResource.Status);

        // AND the command service received a DismissAlertCommand with the reason
        await _alertCommandService.Received(1).Handle(
            Arg.Is<DismissAlertCommand>(c => c.AlertId == 60L && c.Reason == "False positive after field inspection."),
            Arg.Any<CancellationToken>());
    }

    // ---- helpers ----

    private static AlertSummaryResource NewSummary(
        long id,
        EAlertSeverity severity,
        EThreatType type,
        string status) =>
        new(
            Id: id,
            Type: type.ToString(),
            Description: $"Alert {id}",
            Severity: severity.ToString(),
            Date: DateTimeOffset.UtcNow.ToString("O"),
            Status: status,
            Sources: new List<string>(),
            PlotId: 100L,
            Plot: new PlotSummaryResource(
                Name: "Test Plot",
                Location: "Test Location",
                Hectares: 1.5
            )
        );

    private static Alert BuildAlert(long id, string status, EAlertSeverity severity)
    {
        var command = new CreateAlertCommand(
            PlotId: 100L,
            AlertType: EThreatType.PHENOLOGICAL_RISK.ToString(),
            Severity: severity.ToString(),
            Title: $"Alert {id}",
            RiskExplanation: "Some risk",
            Sources: new List<string>(),
            DataProviders: new List<string>(),
            SupportingData: new Dictionary<string, string>());

        // The aggregate is constructed with a hard-coded ACTIVE status.
        // For test purposes we just need an Alert with the desired
        // status/severity, so we set the backing fields via reflection
        // (the properties have private setters).
        var alert = new Alert(command);
        SetBackingField(alert, "Id", id);
        SetBackingField(alert, "Status", status);
        SetBackingField(alert, "Severity", severity);
        return alert;
    }

    private static void SetBackingField(Alert alert, string propertyName, object value)
    {
        // Auto-properties with private setters expose a <PropertyName>k__BackingField
        // field. We poke through reflection because that's exactly what EF does
        // at materialization time.
        var backingField = typeof(Alert).GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(backingField);
        backingField!.SetValue(alert, value);
    }
}
