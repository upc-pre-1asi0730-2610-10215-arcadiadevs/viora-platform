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

namespace ArcadiaDevs.Viora.Platform.Tests.Surveillance.Interfaces.Rest.Controllers;

/// <summary>
///     WU5 tests for <see cref="PestSightingReportsController"/> PATCH
///     endpoint. Verifies the happy path (review returns 200) and the
///     error path (invalid outcome returns 400 with ProblemDetails).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class PestSightingReportsControllerTests
{
    private readonly IPestSightingCommandService _commandService = Substitute.For<IPestSightingCommandService>();
    private readonly IPestSightingReportQueryService _queryService = Substitute.For<IPestSightingReportQueryService>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer = StubLocalizer();
    private readonly ProblemDetailsFactory _problemDetailsFactory = new TestProblemDetailsFactory();

    private static IStringLocalizer<ErrorMessages> StubLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<ErrorMessages>>();
        localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), value: call.ArgAt<string>(0)));
        return localizer;
    }

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

    private PestSightingReportsController CreateController()
    {
        var controller = new PestSightingReportsController(
            _commandService,
            _queryService,
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

    /// <summary>
    ///     Builds a <see cref="PestSightingReport"/> with a known Id
    ///     via reflection (mirrors how EF Core sets the Id at materialisation).
    /// </summary>
    private static PestSightingReport BuildReport(int id, long reporterUserId, EReportStatus status)
    {
        var command = new CreatePestSightingReportCommand(
            PlotId: 42L,
            ReporterUserId: reporterUserId,
            RiskZone: "FULL_PLOT",
            Symptoms: new List<string> { "yellowing leaves" },
            ObservedSeverity: "LOW",
            Notes: "Test");

        var report = new PestSightingReport(command);

        // Set Id via backing field (get-only auto-property)
        var idField = typeof(PestSightingReport).GetField(
            "<Id>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        idField?.SetValue(report, id);

        // Transition to the desired status
        switch (status)
        {
            case EReportStatus.CONFIRMED:
                report.ConfirmAfterInspection();
                break;
            case EReportStatus.RULED_OUT:
                report.DismissAfterInspection();
                break;
            // UNDER_REVIEW and NEEDS_INSPECTION require EvaluateBiologicalRisk to set
            case EReportStatus.NEEDS_INSPECTION:
                report.EvaluateBiologicalRisk(0.50, EThreatType.PEST_SYMPTOM);
                break;
        }

        return report;
    }

    [Fact]
    public async Task Patch_ReviewAction_Returns200()
    {
        // GIVEN a command service that returns a successful review
        var report = BuildReport(10, 100L, EReportStatus.CONFIRMED);
        _commandService.Handle(Arg.Any<ReviewPestSightingReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result<PestSightingReport, Error>.Success(report));

        var controller = CreateController();

        // WHEN the controller PATCHes the report with outcome CONFIRMED
        var resource = new ReviewPestSightingReportResource(Outcome: "CONFIRMED");
        var result = await controller.ReviewPestSightingReport(
            reportId: 10, reporterUserId: 100L, resource, CancellationToken.None);

        // THEN the result is 200 OK with the reviewed report resource
        var ok = Assert.IsType<OkObjectResult>(result);
        var pestResource = Assert.IsType<PestSightingReportResource>(ok.Value);
        Assert.Equal(10L, pestResource.Id);
    }

    [Fact]
    public async Task Patch_ReviewAction_InvalidState_Returns400()
    {
        // GIVEN a command service that returns a failure (invalid outcome)
        _commandService.Handle(Arg.Any<ReviewPestSightingReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result<PestSightingReport, Error>.Failure(
                new Error("Surveillance.InvalidOutcome", "Outcome must be CONFIRMED or RULED_OUT.")));

        var controller = CreateController();

        // WHEN the controller PATCHes with an invalid outcome
        var resource = new ReviewPestSightingReportResource(Outcome: "INVALID");
        var result = await controller.ReviewPestSightingReport(
            reportId: 10, reporterUserId: 100L, resource, CancellationToken.None);

        // THEN the result is 400 Bad Request with ProblemDetails
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Outcome must be CONFIRMED or RULED_OUT", problemDetails.Title);
    }
}
