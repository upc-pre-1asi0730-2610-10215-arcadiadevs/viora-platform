# IAM Opt-In Pattern

This document explains how other bounded contexts (Agronomic, Surveillance) should
consume the IAM bounded context through `IIamContextFacade`. Adding `[Authorize]`
to existing controllers is a **separate per-controller change** — not part of the
current IAM slices.

## TL;DR

- Inject `IIamContextFacade` into your service or controller.
- Call `ExistsUserAsync(userId)` for ownership checks.
- Call `GetUserRolesAsync(userId)` for role-based decisions.
- The `app.UseRequestAuthorization()` middleware is already wired in the pipeline;
  no further pipeline changes are needed.

## How to use the facade

```csharp
public class SomeSurveillanceService(IIamContextFacade iam)
{
    public async Task<Result> DoSomethingAsync(int userId, CancellationToken ct)
    {
        if (!await iam.ExistsUserAsync(userId, ct))
            return Result.Failure(SomeError.UnknownUser);

        var roles = await iam.GetUserRolesAsync(userId, ct);
        if (!roles.Contains("Administrator"))
            return Result.Failure(SomeError.InsufficientPrivileges);

        // … proceed
    }
}
```

### Ownership guard

```csharp
if (!await _iam.ExistsUserAsync(request.UserId, ct))
    return NotFound();
```

### Role-based guard

```csharp
var roles = await _iam.GetUserRolesAsync(currentUserId, ct);
if (!roles.Contains("PhytosanitarySpecialist"))
    return Forbid();
```

## Adding `[Authorize]` to controllers

The custom `AuthorizeAttribute` supports role filtering:

```csharp
[Authorize(Roles = "Administrator")]
[HttpPost("some-sensitive-action")]
public async Task<IActionResult> SomeSensitiveAction() { … }
```

Adding this attribute to an existing Agronomic or Surveillance controller is a
**separate per-controller follow-up change**. Each controller must be evaluated
individually:

1. Does it need authentication at all?
2. Which roles should be allowed?
3. Are there any `[AllowAnonymous]` endpoints that must stay open?

Do **not** add `[Authorize]` to controllers as part of the IAM auth-jwt change.

## Pipeline

The `RequestAuthorizationMiddleware` is already registered in `Program.cs`:

```csharp
app.UseRequestAuthorization();
```

This runs after `UseRouting()` and before `MapControllers()`. No additional
pipeline configuration is required for the facade to work.

## DI Registration

```csharp
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();
```

This is already registered. Inject `IIamContextFacade` anywhere within the same
DI container scope.
