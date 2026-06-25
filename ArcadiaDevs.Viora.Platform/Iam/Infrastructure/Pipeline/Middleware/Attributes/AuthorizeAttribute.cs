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
 * <summary>
 *     This attribute is used to decorate controllers and actions that require authorization.
 *     It checks if the user is authenticated by checking if HttpContext.Items["User"] is set.
 *     If a user is not signed in, then it returns a 401 ProblemDetails response.
 *     When <see cref="Roles"/> is set, the user must have at least one of the comma-separated roles.
 * </summary>
 */
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /**
     * <summary>
     *     Gets or sets a comma-separated list of roles that are allowed to access the resource.
     *     If set, the user must have at least one of the specified roles (OR semantics).
     *     If not set, any authenticated user is allowed (S1 behavior).
     * </summary>
     */
    public string? Roles { get; set; }

    /**
     * <summary>
     *     This method is called when authorization is required.
     *     It checks if the user is logged in by checking if HttpContext.Items["User"] is set.
     *     If <see cref="Roles"/> is set, it also checks the user's role claims.
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
            WriteProblemDetails(context, StatusCodes.Status401Unauthorized, IamErrors.TokenRequired);
            return;
        }

        // S2: Role-based authorization — if Roles is set, check user has at least one match
        if (!string.IsNullOrEmpty(Roles))
        {
            var requiredRoles = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var principal = context.HttpContext.User;

            foreach (var role in requiredRoles)
            {
                if (principal.IsInRole(role))
                    return; // Match found — allow
            }

            // No matching role — 403 Forbidden
            WriteProblemDetails(context, StatusCodes.Status403Forbidden, IamErrors.InsufficientRole);
        }
    }

    private static void WriteProblemDetails(AuthorizationFilterContext context, int statusCode, Error error)
    {
        var errorLocalizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<ErrorMessages>>();
        var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            context.HttpContext,
            statusCode,
            error.Code,
            errorLocalizer[error.Code].Value ?? error.Message);

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
