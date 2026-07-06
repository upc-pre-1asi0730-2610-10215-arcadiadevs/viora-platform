using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;

/// <summary>
///     Maps <see cref="Result{TValue, TError}" /> to <see cref="ProblemDetails" />
///     for every Billing controller (REQ-CC-3). Mirrors
///     <c>InterventionActionResultAssembler</c>'s shape verbatim. Shared
///     across all 9 Billing controllers (WU1-WU9). WU5 extends
///     <see cref="ToStatusCodeFromErrorCode" /> with a
///     <c>PaymentGatewayNotConfigured</c> → 503 case (REQ-GATE-3) — the
///     first Billing error code needing that status; no other bounded
///     context in this codebase maps to 503 either, so this is a new
///     switch-case entry, not a borrowed pattern.
/// </summary>
public static class BillingActionResultAssembler
{
    private static int ToStatusCodeFromErrorCode(string code)
    {
        return code switch
        {
            _ when code == BillingErrors.NotFound.Code => StatusCodes.Status404NotFound,
            _ when code == BillingErrors.ValidationError.Code => StatusCodes.Status400BadRequest,
            _ when code == BillingErrors.ConflictError.Code => StatusCodes.Status409Conflict,
            _ when code == BillingErrors.DatabaseError.Code
                || code == BillingErrors.InternalServerError.Code => StatusCodes.Status500InternalServerError,
            _ when code == BillingErrors.PaymentGatewayNotConfigured.Code => StatusCodes.Status503ServiceUnavailable,
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
