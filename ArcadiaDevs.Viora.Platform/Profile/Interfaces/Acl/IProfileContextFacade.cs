using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;

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
    ///     created with the given <paramref name="role" /> (defaults to
    ///     <c>Role=Producer</c>) and the provided field values. Idempotent:
    ///     no-op if a profile already exists.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="email">The user's email.</param>
    /// <param name="phone">Optional phone number (null at signup, filled later via PUT).</param>
    /// <param name="role">The role to provision the profile with if it does not already exist.</param>
    /// <param name="ct">The cancellation token.</param>
    Task EnsureProfile(
        int userId,
        string fullName,
        string email,
        string? phone = null,
        ProfileRole role = ProfileRole.Producer,
        CancellationToken ct = default);

    /// <summary>
    ///     Reads a read-only cross-boundary projection of the profile for the
    ///     given user id. Added to support the Intervention BC's
    ///     <c>Specialist</c> aggregate (which resolves identity/contact live
    ///     from the referenced Profile instead of duplicating it — design
    ///     decision 1, obs #267), but usable by any BC needing a read-only
    ///     Profile projection.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The profile summary if a profile exists for the user; otherwise null.</returns>
    Task<ProfileSummary?> GetProfileSummaryAsync(int userId, CancellationToken ct = default);

    /// <summary>
    ///     Permanently deletes the profile for the given user, if one exists.
    ///     Idempotent: no-op if no profile exists for the user. Called by
    ///     Iam's account-deletion flow (matches OS's
    ///     <c>ProfileContextFacade.deleteByUserId</c>).
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="ct">The cancellation token.</param>
    Task DeleteByUserIdAsync(int userId, CancellationToken ct = default);

    Task<string?> GetDisplayNameAsync(int userId, CancellationToken ct = default);

    Task<string?> GetPhotoUrlAsync(int userId, CancellationToken ct = default);
}
