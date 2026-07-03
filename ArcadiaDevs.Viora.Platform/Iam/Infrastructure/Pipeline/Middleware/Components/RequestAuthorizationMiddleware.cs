using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;

/**
 * RequestAuthorizationMiddleware is a custom middleware.
 * This middleware is used to authorize requests.
 * It validates a token is included in the request header and that the token is valid.
 * If the token is valid, it loads the user from the database and sets it in HttpContext.Items["User"].
 */
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    /**
     * InvokeAsync is called by the ASP.NET Core runtime.
     * It validates the token and sets the user in the HttpContext.
     */
    public async Task InvokeAsync(
        HttpContext context,
        IUserQueryService userQueryService,
        ITokenService tokenService,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ProblemDetailsFactory problemDetailsFactory,
        ILogger<RequestAuthorizationMiddleware> logger,
        IHostEnvironment environment)
    {
        var cancellationToken = context.RequestAborted;

        // Swagger UI needs to be reachable without a bearer token, but only in Development —
        // Program.cs also enables it in Staging, and that must stay behind auth.
        if (environment.IsDevelopment() && context.Request.Path.StartsWithSegments("/swagger"))
        {
            await next(context);
            return;
        }

        // Skip authorization if endpoint is decorated with [AllowAnonymous] attribute
        var endpoint = context.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata
            .Any(m => m.GetType() == typeof(AllowAnonymousAttribute)) ?? false;

        if (allowAnonymous)
        {
            await next(context);
            return;
        }

        // Extract token from Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader))
        {
            logger.LogDebug("No Authorization header found for {Path}", context.Request.Path);
            await IamActionResultAssembler.HandleErrorAsync(
                context, IamErrors.TokenRequired, errorLocalizer, problemDetailsFactory);
            return;
        }

        var parts = authHeader.Split(' ');
        if (parts.Length != 2 || !string.Equals(parts[0], "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("Malformed Authorization header for {Path}", context.Request.Path);
            await IamActionResultAssembler.HandleErrorAsync(
                context, IamErrors.TokenMalformed, errorLocalizer, problemDetailsFactory);
            return;
        }

        var token = parts[1];
        if (string.IsNullOrEmpty(token))
        {
            logger.LogDebug("Empty token in Authorization header for {Path}", context.Request.Path);
            await IamActionResultAssembler.HandleErrorAsync(
                context, IamErrors.TokenMalformed, errorLocalizer, problemDetailsFactory);
            return;
        }

        // Validate token
        var validation = await tokenService.ValidateToken(token);

        if (!validation.IsValid)
        {
            var error = validation.FailureCode == "Iam.TokenExpired"
                ? IamErrors.TokenExpired
                : IamErrors.TokenInvalid;
            logger.LogDebug("Token validation failed ({FailureCode}) for {Path}", validation.FailureCode, context.Request.Path);
            await IamActionResultAssembler.HandleErrorAsync(
                context, error, errorLocalizer, problemDetailsFactory);
            return;
        }

        // Load user from database (per design: DB lookup on every authenticated request)
        var user = await userQueryService.Handle(new GetUserByIdQuery(validation.UserId!.Value), cancellationToken);

        if (user == null)
        {
            logger.LogDebug("User {UserId} from token not found in database for {Path}", validation.UserId.Value, context.Request.Path);
            await IamActionResultAssembler.HandleErrorAsync(
                context, IamErrors.UserNotFound, errorLocalizer, problemDetailsFactory);
            return;
        }

        // Set user in HttpContext for downstream use
        context.Items["User"] = user;
        context.Items["UserId"] = user.Id;
        context.Items["Username"] = user.Username;

        // Populate HttpContext.User with claims for role-based authorization
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Sid, user.Id.ToString()),
            new(System.Security.Claims.ClaimTypes.Name, user.Username),
        };
        // Add role claims so IsInRole() works as expected
        foreach (var role in user.Roles)
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role.Name));

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "jwt");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        logger.LogDebug("User {UserId} authorized for {Path}", user.Id, context.Request.Path);

        await next(context);
    }
}
