using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/**
 * <summary>
 *     Marks an action parameter as bound from the authenticated caller's own id,
 *     derived from the JWT by <c>RequestAuthorizationMiddleware</c> — never from a
 *     client-supplied path, query, or body value. Use this instead of
 *     <c>[FromQuery] int userId</c> / <c>[FromRoute] int growerId</c> wherever the
 *     endpoint needs to know "who is calling", so ownership checks compare against
 *     an id that cannot be spoofed.
 * </summary>
 */
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromTokenAttribute() : ModelBinderAttribute(typeof(CurrentUserIdModelBinder));
