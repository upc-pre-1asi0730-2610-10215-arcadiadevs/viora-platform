using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;

/// <summary>
///     Maps <see cref="Result{TValue, TError}" /> to <see cref="ProblemDetails" />
///     for every Intervention controller (REQ-CC-2). Copied from
///     <c>AlertsController</c>'s Result→ProblemDetails shape, generalized to
///     a single generic method (rather than one per aggregate type like
///     <c>SurveillanceActionResultAssembler</c>) since this assembler is
///     shared across all 9 future Intervention controllers (WU1-WU8).
/// </summary>
public static class InterventionActionResultAssembler
{
    private static int ToStatusCode(string code)
    {
        if (code == InterventionErrors.NotFound.Code)
        {
            return StatusCodes.Status404NotFound;
        }

        if (code == InterventionErrors.ValidationError.Code)
        {
            return StatusCodes.Status400BadRequest;
        }

        if (code == InterventionErrors.ConflictError.Code)
        {
            return StatusCodes.Status409Conflict;
        }

        if (code == InterventionErrors.ContactNotUnlocked.Code)
        {
            return StatusCodes.Status403Forbidden;
        }

        if (code == InterventionErrors.DatabaseError.Code || code == InterventionErrors.InternalServerError.Code)
        {
            return StatusCodes.Status500InternalServerError;
        }

        return StatusCodes.Status400BadRequest;
    }

    /// <summary>
    ///     Maps a <see cref="Result{TValue, TError}" /> to an <see cref="IActionResult" />,
    ///     invoking <paramref name="onSuccess" /> on success or building a
    ///     <see cref="ProblemDetails" /> response on failure.
    /// </summary>
    public static IActionResult ToActionResult<TValue>(
        ControllerBase controller,
        Result<TValue, Error> result,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        Func<TValue, IActionResult> onSuccess)
    {
        if (result is Result<TValue, Error>.Success success)
        {
            return onSuccess(success.Value);
        }

        var failure = (Result<TValue, Error>.Failure)result;
        var statusCode = ToStatusCode(failure.Error.Code);
        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            statusCode,
            failure.Error.Code,
            errorLocalizer[failure.Error.Code].Value ?? failure.Error.Message);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
