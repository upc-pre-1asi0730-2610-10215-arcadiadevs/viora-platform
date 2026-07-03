using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Transform;

/// <summary>
///     Centralizes the translation of Profile <see cref="Result{T,Error}" /> outcomes
///     into RFC 7807 <see cref="ProblemDetails" /> action results.
/// </summary>
public static class ProfileActionResultAssembler
{
    private static int ToStatusCodeFromErrorCode(string code)
    {
        return code switch
        {
            "Profile.NotFound" => StatusCodes.Status404NotFound,
            "Profile.ProfileAlreadyExists" => StatusCodes.Status409Conflict,
            "Profile.CreationFailed" => StatusCodes.Status500InternalServerError,
            _ when code.StartsWith("Profile.") => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
    }

    /// <summary>
    ///     Translates a <see cref="Result{T,Error}" /> into an <see cref="IActionResult" />.
    /// </summary>
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
