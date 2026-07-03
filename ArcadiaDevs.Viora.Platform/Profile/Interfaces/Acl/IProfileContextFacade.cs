namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;

/// <summary>
///     ACL facade for cross-boundary profile provisioning.
/// </summary>
/// <remarks>
///     IAM's signup flow calls <see cref="EnsureProfile" /> after user
///     persistence to create a default profile. The interface lives under
///     <c>Profile/Interfaces/Acl</c> so the IAM BC depends only on this
///     contract, not on Profile's internal types.
/// </remarks>
public interface IProfileContextFacade
{
    /// <summary>
    ///     Ensures a profile exists for the given user. If none exists, one is
    ///     created with <c>Role=Producer</c> and the provided field values.
    ///     Idempotent: no-op if a profile already exists.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="phone">Optional phone number (null at signup, filled later via PUT).</param>
    /// <param name="ct">The cancellation token.</param>
    Task EnsureProfile(
        int userId,
        string fullName,
        string email,
        string? phone = null,
        CancellationToken ct = default);
}
