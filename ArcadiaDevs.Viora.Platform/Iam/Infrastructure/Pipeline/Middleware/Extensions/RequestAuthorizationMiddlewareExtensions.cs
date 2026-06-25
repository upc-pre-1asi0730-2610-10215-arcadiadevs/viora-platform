using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Extensions;

/**
 * <summary>
 *     Middleware extensions for IAM authorization.
 * </summary>
 */
public static class RequestAuthorizationMiddlewareExtensions
{
    /**
     * <summary>
     *     Registers the request authorization middleware.
     * </summary>
     * <param name="builder">The application builder</param>
     * <returns>The application builder</returns>
     */
    public static IApplicationBuilder UseRequestAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestAuthorizationMiddleware>();
    }
}
