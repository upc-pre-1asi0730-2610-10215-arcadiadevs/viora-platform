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
///     a single generic method — matching the majority convention already
///     used by <c>ProfileActionResultAssembler</c> and
///     <c>AgronomicActionResultAssembler</c> (not a novel choice), rather
///     than <c>SurveillanceActionResultAssembler</c>'s per-aggregate-type
///     duplication. This assembler is shared across all 9 future
///     Intervention controllers (WU1-WU8).
/// </summary>
public static class InterventionActionResultAssembler
{
    private static int ToStatusCodeFromErrorCode(string code)
    {
        return code switch
        {
            _ when code == InterventionErrors.NotFound.Code => StatusCodes.Status404NotFound,
            _ when code == InterventionErrors.ValidationError.Code => StatusCodes.Status400BadRequest,
            _ when code == InterventionErrors.ConflictError.Code => StatusCodes.Status409Conflict,
            _ when code == InterventionErrors.ContactNotUnlocked.Code => StatusCodes.Status403Forbidden,
            _ when code == InterventionErrors.DatabaseError.Code
                || code == InterventionErrors.InternalServerError.Code => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
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
        var statusCode = ToStatusCodeFromErrorCode(failure.Error.Code);
        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            statusCode,
            failure.Error.Code,
            errorLocalizer[failure.Error.Code].Value ?? failure.Error.Message);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
