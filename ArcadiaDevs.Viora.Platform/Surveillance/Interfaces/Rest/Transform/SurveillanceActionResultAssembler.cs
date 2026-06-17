using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Http;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;

public static class SurveillanceActionResultAssembler
{
    private static int ToStatusCodeFromErrorCode(string code)
    {
        return code switch
        {
            "ReportNotFound" => StatusCodes.Status404NotFound,
            "AlertNotFound" => StatusCodes.Status404NotFound,
            "SymptomNotFound" => StatusCodes.Status404NotFound,
            "InvalidRiskZone" => StatusCodes.Status400BadRequest,
            "AlertAlreadyReviewed" => StatusCodes.Status409Conflict,
            "DatabaseError" => StatusCodes.Status500InternalServerError,
            "InternalServerError" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }

    public static IActionResult ToActionResultFromCreatePestSightingReportResult(
        ControllerBase controller,
        Result<PestSightingReport, Error> result,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<PestSightingReport, IActionResult> successAction)
    {
        if (result is Result<PestSightingReport, Error>.Success success)
            return successAction(success.Value);

        var failure = (Result<PestSightingReport, Error>.Failure)result;
        var statusCode = ToStatusCodeFromErrorCode(failure.Error.Code);
        return problemDetailsFactory.CreateProblemDetails(
            controller, 
            statusCode, 
            failure.Error.Code, 
            errorLocalizer[failure.Error.Code].Value ?? failure.Error.Message);
    }

    public static IActionResult ToActionResultFromCreateAlertResult(
        ControllerBase controller,
        Result<Alert, Error> result,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<Alert, IActionResult> successAction)
    {
        if (result is Result<Alert, Error>.Success success)
            return successAction(success.Value);

        var failure = (Result<Alert, Error>.Failure)result;
        var statusCode = ToStatusCodeFromErrorCode(failure.Error.Code);
        return problemDetailsFactory.CreateProblemDetails(
            controller, 
            statusCode, 
            failure.Error.Code, 
            errorLocalizer[failure.Error.Code].Value ?? failure.Error.Message);
    }

    public static IActionResult ToActionResultFromGetAllSymptomsResult(
        ControllerBase controller,
        IEnumerable<SymptomDictionaryItem> symptoms,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<IEnumerable<SymptomDictionaryItem>, IActionResult> successAction)
    {
        return successAction(symptoms);
    }
}
