using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Centralizes the translation of Agronomic <see cref="Result{T,Error}"/> outcomes
///     into RFC 7807 <see cref="ProblemDetails"/> action results.
/// </summary>
public static class AgronomicActionResultAssembler
{
    private static int ToStatusCodeFromErrorCode(string code)
    {
        return code switch
        {
            "Agronomic.PlotNotFound" => StatusCodes.Status404NotFound,
            "Agronomic.DeviceNotFound" => StatusCodes.Status404NotFound,
            "Agronomic.PlanNotFound" => StatusCodes.Status404NotFound,
            "Agronomic.TileNotFound" => StatusCodes.Status404NotFound,
            "Agronomic.UnauthorizedAccess" => StatusCodes.Status403Forbidden,
            "Agronomic.PlotOwnership" => StatusCodes.Status403Forbidden,
            "Agronomic.AgronomicStatisticsAccess" => StatusCodes.Status403Forbidden,
            "Agronomic.PlotNotLinked" => StatusCodes.Status400BadRequest,
            "Agronomic.PlotInactive" => StatusCodes.Status400BadRequest,
            "Agronomic.CertificationError" => StatusCodes.Status400BadRequest,
            "Agronomic.GenerationError" => StatusCodes.Status400BadRequest,
            "Agronomic.PlotConflict" => StatusCodes.Status409Conflict,
            "Agronomic.DeleteActivePlot" => StatusCodes.Status409Conflict,
            "Agronomic.InvalidInput" => StatusCodes.Status400BadRequest,
            "Agronomic.InvalidState" => StatusCodes.Status400BadRequest,
            "Agronomic.PlanNotCertifiable" => StatusCodes.Status422UnprocessableEntity,
            "Agronomic.InternalServerError" => StatusCodes.Status500InternalServerError,
            "Agronomic.QueryError" => StatusCodes.Status500InternalServerError,
            _ when code.StartsWith("Agronomic.") => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }

    public static IActionResult ToActionResult<T>(
        ControllerBase controller,
        Result<T, Error> result,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<T, IActionResult> successAction)
    {
        if (result is Result<T, Error>.Success success)
            return successAction(success.Value);

        var failure = (Result<T, Error>.Failure)result;
        var statusCode = ToStatusCodeFromErrorCode(failure.Error.Code);
        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            statusCode,
            failure.Error.Code,
            errorLocalizer[failure.Error.Code].Value ?? failure.Error.Message);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
