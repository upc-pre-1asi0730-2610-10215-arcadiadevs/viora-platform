using System.Text.Json.Serialization;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;

/**
 * <summary>
 *     The user aggregate
 * </summary>
 * <remarks>
 *     This class is used to represent a user
 * </remarks>
 */
public partial class User(
    string username,
    string passwordHash,
    string? email = null,
    string? fullName = null,
    bool verified = true)
{
    public User() : this(string.Empty, string.Empty)
    {
    }

    public int Id { get; }
    public string Username { get; private set; } = username;

    [JsonIgnore] public string PasswordHash { get; private set; } = passwordHash;

    /**
     * <summary>
     *     The user's email address (OS parity). Internal to Iam only — used for
     *     verification/notification, never surfaced on the public UserResource.
     *     Nullable: pre-existing rows have no backfilled value (out of scope).
     * </summary>
     */
    public string? Email { get; private set; } = email;

    /**
     * <summary>
     *     The user's full name (OS parity). Internal to Iam only. Nullable:
     *     pre-existing rows have no backfilled value (out of scope).
     * </summary>
     */
    public string? FullName { get; private set; } = fullName;

    /**
     * <summary>
     *     Whether this account is active. Defaults <c>true</c> — deactivation is
     *     opt-in (REQ-DEACT-1).
     * </summary>
     */
    public bool Active { get; private set; } = true;

    /**
     * <summary>
     *     Whether this account's email has been verified. Defaults <c>true</c>
     *     (safe backfill for pre-existing rows / EF materialization) — new
     *     signups explicitly pass <c>verified: false</c> (REQ-AUTH-1).
     * </summary>
     */
    public bool Verified { get; private set; } = verified;

    /**
     * <summary>
     *     The roles assigned to this user
     * </summary>
     * <remarks>
     *     This is an EF Core skip-navigation — no UserRole entity class exists.
     *     The join table user_roles is generated automatically by EF Core.
     * </remarks>
     */
    public ICollection<Role> Roles { get; private set; } = [];

    /**
     * <summary>
     *     Update the username
     * </summary>
     * <param name="username">The new username</param>
     * <returns>The updated user</returns>
     */
    public User UpdateUsername(string username)
    {
        Username = username;
        return this;
    }

    /**
     * <summary>
     *     Update the password hash
     * </summary>
     * <param name="passwordHash">The new password hash</param>
     * <returns>The updated user</returns>
     */
    public User UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        return this;
    }

    /**
     * <summary>
     *     Update the full name (keeps the account's display name in sync with
     *     the profile — see <c>IIamContextFacade.UpdateFullNameAsync</c>).
     * </summary>
     * <param name="fullName">The new full name</param>
     * <returns>The updated user</returns>
     */
    public User UpdateFullName(string fullName)
    {
        FullName = fullName;
        return this;
    }

    /**
     * <summary>
     *     Marks the account's email as verified (REQ-EV-2). Plain mutator — no
     *     re-verify guard exists on this aggregate; the "already consumed"
     *     protection lives on <see cref="VerificationToken.Consume" /> instead.
     * </summary>
     */
    public void MarkVerified()
    {
        Verified = true;
    }

    /**
     * <summary>
     *     Deactivates the account (REQ-DEACT-2). Self-guarded — only succeeds
     *     when currently <c>Active</c>; re-deactivating an already-deactivated
     *     user is a conflict (REQ-DEACT-3).
     * </summary>
     */
    public Result<Unit, Error> Deactivate()
    {
        if (!Active)
            return new Result<Unit, Error>.Failure(IamErrors.UserAlreadyDeactivated);

        Active = false;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /**
     * <summary>
     *     Activates the account. Self-guarded, symmetric with
     *     <see cref="Deactivate" /> — kept for domain symmetry; no REST endpoint
     *     invokes this in this change's scope.
     * </summary>
     */
    public Result<Unit, Error> Activate()
    {
        if (Active)
            return new Result<Unit, Error>.Failure(IamErrors.UserAlreadyDeactivated);

        Active = true;
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
