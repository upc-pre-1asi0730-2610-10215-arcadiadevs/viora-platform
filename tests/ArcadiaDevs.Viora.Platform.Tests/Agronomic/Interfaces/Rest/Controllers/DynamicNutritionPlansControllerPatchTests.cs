using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;
using NutritionInputRecommendation = ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects.NutritionInputRecommendation;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     Unit tests for <see cref="DynamicNutritionPlansController"/> PATCH route (certify).
///     Template C: controller tests with a fake <see cref="HttpContext"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class DynamicNutritionPlansControllerPatchTests
{
    private readonly IRecommendDynamicNutritionPlanCommandService _recommendService =
        Substitute.For<IRecommendDynamicNutritionPlanCommandService>();
    private readonly ICertifyNutritionApplicationCommandService _certifyService =
        Substitute.For<ICertifyNutritionApplicationCommandService>();
    private readonly IDynamicNutritionQueryService _queryService =
        Substitute.For<IDynamicNutritionQueryService>();
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

    private DynamicNutritionPlansController CreateController()
    {
        var controller = new DynamicNutritionPlansController(
            _recommendService,
            _certifyService,
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
    ///     Builds a valid <see cref="DynamicNutritionPlan"/> for testing.
    /// </summary>
    private static DynamicNutritionPlan BuildPlan()
    {
        var inputRecommendations = new NutritionInputRecommendation[]
        {
            new(
                "NPK 20-20-20",
                "Primary nutrient",
                100.0,
                "kg/ha",
                ENutritionInputStatus.Recommended)
        };

        var applicationWindow = new NutritionApplicationWindow(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7));

        var rationale = new PlanRationale(
            "Test plan rationale",
            EClimateRiskLevel.Moderate,
            new NdviValue(0.72),
            temperatureAnomaly: 2.5);

        return DynamicNutritionPlan.Recommend(
            userId: 1,
            plotId: 10,
            inputRecommendations,
            applicationWindow,
            rationale,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    ///     Builds a valid <see cref="CertifyNutritionApplicationResource"/> for testing.
    /// </summary>
    private static CertifyNutritionApplicationResource BuildCertificationResource()
    {
        return new CertifyNutritionApplicationResource(
            UserId: 1,
            ApplicationDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ApplicationTime: new TimeOnly(10, 0),
            AppliedInputs: new[] { "NPK 20-20-20" },
            DoseConfirmation: "CONFIRMED",
            FieldOperator: "Juan Perez",
            FieldNotes: "Applied in the morning");
    }

    /// <summary>
    ///     GIVEN a valid certification request for an active plan
    ///     WHEN PATCH /api/v1/dynamic-nutrition-plans/{planId} is called
    ///     THEN the response is 200 OK with the certified plan resource.
    /// </summary>
    [Fact]
    public async Task Patch_ValidCertification_Returns200()
    {
        // GIVEN a valid certification request for an active plan
        var plan = BuildPlan();
        _certifyService.Handle(Arg.Any<CertifyNutritionApplicationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DynamicNutritionPlan, Error>.Success(plan));

        var controller = CreateController();
        var resource = BuildCertificationResource();

        // WHEN PATCH /api/v1/dynamic-nutrition-plans/1?userId=1
        var result = await controller.CertifyDynamicNutritionPlan(
            userId: 1, planId: 1, resource, CancellationToken.None);

        // THEN the result is 200 OK with the plan resource
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<DynamicNutritionPlanResource>(ok.Value);
    }

    /// <summary>
    ///     GIVEN a certification request for a plan that is not certifiable
    ///     WHEN PATCH /api/v1/dynamic-nutrition-plans/{planId} is called
    ///     THEN the response is 422 Unprocessable Entity.
    /// </summary>
    [Fact]
    public async Task Patch_InvalidState_Returns422()
    {
        // GIVEN the plan is in a non-certifiable state (e.g., already superseded)
        _certifyService.Handle(Arg.Any<CertifyNutritionApplicationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DynamicNutritionPlan, Error>.Failure(AgronomicErrors.PlanNotCertifiable));

        var controller = CreateController();
        var resource = BuildCertificationResource();

        // WHEN PATCH /api/v1/dynamic-nutrition-plans/1?userId=1
        var result = await controller.CertifyDynamicNutritionPlan(
            userId: 1, planId: 1, resource, CancellationToken.None);

        // THEN the result is 422 Unprocessable Entity
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }

    /// <summary>
    ///     GIVEN a certification request for a non-existent plan
    ///     WHEN PATCH /api/v1/dynamic-nutrition-plans/{planId} is called
    ///     THEN the response is 404 Not Found.
    /// </summary>
    [Fact]
    public async Task Patch_PlanNotFound_Returns404()
    {
        // GIVEN the plan does not exist
        _certifyService.Handle(Arg.Any<CertifyNutritionApplicationCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result<DynamicNutritionPlan, Error>.Failure(AgronomicErrors.PlanNotFound));

        var controller = CreateController();
        var resource = BuildCertificationResource();

        // WHEN PATCH /api/v1/dynamic-nutrition-plans/999?userId=1
        var result = await controller.CertifyDynamicNutritionPlan(
            userId: 1, planId: 999, resource, CancellationToken.None);

        // THEN the result is 404 Not Found
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.IsType<ProblemDetails>(objectResult.Value);
    }
}
