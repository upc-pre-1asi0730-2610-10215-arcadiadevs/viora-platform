using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/**
 * <summary>
 *     Binds an <see cref="int" /> or <see cref="long" /> action parameter from the
 *     caller's own identity, as resolved by <c>RequestAuthorizationMiddleware</c>
 *     from the bearer token (<c>HttpContext.Items["UserId"]</c>) — never from
 *     client-supplied query, route, or body input. This is the WA equivalent of
 *     OS's <c>@CurrentUserId</c> / <c>CurrentUserIdArgumentResolver</c>: identity
 *     always comes from the token.
 * </summary>
 */
public class CurrentUserIdModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var httpContext = bindingContext.ActionContext.HttpContext;

        if (httpContext.Items["UserId"] is int userId)
        {
            // Convert to the target parameter's type (int or long) so a single
            // binder can serve both — the underlying identity is always the
            // same int stored by RequestAuthorizationMiddleware. Each branch
            // must box its own type explicitly: a ternary with an int arm and
            // a long arm forces C#'s common-type conversion to long for BOTH
            // arms (even the one not taken), so `? (long)userId : userId`
            // always boxes a long — silently breaking every `[FromToken] int`
            // parameter's unboxing cast in the action-invocation pipeline.
            object boundValue;
            if (Nullable.GetUnderlyingType(bindingContext.ModelType) == typeof(long)
                || bindingContext.ModelType == typeof(long))
            {
                boundValue = (long)userId;
            }
            else
            {
                boundValue = userId;
            }

            bindingContext.Result = ModelBindingResult.Success(boundValue);
        }
        else
        {
            // No authenticated user on the context — RequestAuthorizationMiddleware
            // did not run (e.g. [AllowAnonymous]) or found no user. Fail binding
            // rather than defaulting to 0, so a missing identity surfaces as a
            // model-state error instead of a spoofable default.
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "No authenticated user id is available on the request.");
            bindingContext.Result = ModelBindingResult.Failed();
        }

        return Task.CompletedTask;
    }
}
