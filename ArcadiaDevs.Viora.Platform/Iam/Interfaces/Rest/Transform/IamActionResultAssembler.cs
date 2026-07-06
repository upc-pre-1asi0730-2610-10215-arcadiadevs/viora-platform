using System.Text.Json;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/// <summary>
///     Centralizes the translation of Iam <see cref="Result{T,Error}"/> outcomes
///     into RFC 7807 <see cref="ProblemDetails"/> action results.
/// </summary>
public static class IamActionResultAssembler
{
    private static int ToStatusCodeFromErrorCode(string code)
    {
        return code switch
        {
            "Iam.InvalidCredentials" => StatusCodes.Status401Unauthorized,
            "Iam.UsernameAlreadyTaken" => StatusCodes.Status400BadRequest,
            "Iam.UserCreationFailed" => StatusCodes.Status500InternalServerError,
            "Iam.WeakPassword" => StatusCodes.Status400BadRequest,
            "Iam.UserNotFound" => StatusCodes.Status404NotFound,
            "Iam.TokenRequired" => StatusCodes.Status401Unauthorized,
            "Iam.TokenMalformed" => StatusCodes.Status401Unauthorized,
            "Iam.TokenInvalid" => StatusCodes.Status401Unauthorized,
            "Iam.TokenExpired" => StatusCodes.Status401Unauthorized,
            "Iam.InvalidRoleName" => StatusCodes.Status400BadRequest,
            "Iam.InvalidCurrentPassword" => StatusCodes.Status400BadRequest,
            "Iam.InsufficientRole" => StatusCodes.Status403Forbidden,
            "Iam.UserDisabled" => StatusCodes.Status403Forbidden,
            "Iam.EmailNotVerified" => StatusCodes.Status422UnprocessableEntity,
            "Iam.VerificationTokenNotFound" => StatusCodes.Status404NotFound,
            "Iam.VerificationTokenExpiredOrConsumed" => StatusCodes.Status400BadRequest,
            "Iam.EmailAlreadyVerified" => StatusCodes.Status422UnprocessableEntity,
            "Iam.SessionNotFound" => StatusCodes.Status404NotFound,
            "Iam.CannotRevokeCurrentSession" => StatusCodes.Status409Conflict,
            "Iam.InvalidAccountStateUpdate" => StatusCodes.Status400BadRequest,
            "Iam.UserAlreadyDeactivated" => StatusCodes.Status409Conflict,
            _ when code.StartsWith("Iam.") => StatusCodes.Status500InternalServerError,
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

    /// <summary>
    ///     Writes a ProblemDetails response directly to the HttpContext and short-circuits the pipeline.
    ///     Used by the request authorization middleware.
    /// </summary>
    public static async Task HandleErrorAsync(
        HttpContext context,
        Error error,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory)
    {
        var statusCode = ToStatusCodeFromErrorCode(error.Code);
        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            context,
            statusCode,
            error.Code,
            errorLocalizer[error.Code].Value ?? error.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
