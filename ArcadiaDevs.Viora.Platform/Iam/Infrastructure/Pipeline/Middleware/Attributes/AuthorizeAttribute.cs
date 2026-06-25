using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/**
 * This attribute is used to decorate controllers and actions that require authorization.
 * It checks if the user is authenticated by checking if HttpContext.Items["User"] is set.
 * If a user is not signed in, then it returns a 401 ProblemDetails response.
 * S1: no role check. Role-based authorization comes in S2.
 */
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /**
     * <summary>
     *     This method is called when authorization is required.
     *     It checks if the user is logged in by checking if HttpContext.Items["User"] is set.
     *     If a user is not signed in then it returns 401 ProblemDetails.
     * </summary>
     * <param name="context">The authorization filter context</param>
     */
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();

        if (allowAnonymous)
            return;

        // Check if a user is present in HttpContext.Items
        var user = context.HttpContext.Items["User"];

        // If no user is found, return 401 ProblemDetails
        if (user == null)
        {
            var errorLocalizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<ErrorMessages>>();
            var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

            var statusCode = StatusCodes.Status401Unauthorized;
            var error = IamErrors.TokenRequired;
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                context.HttpContext,
                statusCode,
                error.Code,
                errorLocalizer[error.Code].Value ?? error.Message);

            context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        }
    }
}
