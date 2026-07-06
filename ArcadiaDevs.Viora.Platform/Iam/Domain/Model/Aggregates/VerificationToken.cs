using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     The purpose a <see cref="VerificationToken" /> was issued for. Only
///     <see cref="VerifyEmail" /> is exercised in this change — <see cref="ResetPassword" />
///     is ported for OS-parity extensibility, unused this phase.
/// </summary>
public enum VerificationTokenPurpose
{
    VerifyEmail,
    ResetPassword
}

/// <summary>
///     The verification-token aggregate — a single-use, expiring token bound
///     to a <see cref="User" /> (REQ-EV-1).
/// </summary>
public partial class VerificationToken(int userId, string token, VerificationTokenPurpose purpose, DateTime expiresAt)
{
    public VerificationToken() : this(0, string.Empty, VerificationTokenPurpose.VerifyEmail, DateTime.MinValue)
    {
    }

    public int Id { get; }
    public int UserId { get; private set; } = userId;
    public string Token { get; private set; } = token;
    public VerificationTokenPurpose Purpose { get; private set; } = purpose;
    public DateTime ExpiresAt { get; private set; } = expiresAt;
    public bool Consumed { get; private set; }

    /// <summary>
    ///     Issues a new email-verification token with a 24-hour expiry.
    /// </summary>
    public static VerificationToken IssueEmailVerification(int userId, DateTime now) =>
        new(userId, Guid.NewGuid().ToString(), VerificationTokenPurpose.VerifyEmail, now.AddHours(24));

    /// <summary>
    ///     Whether the token's expiry has passed relative to <paramref name="now" />
    ///     (caller sources it from <c>IClock</c>, not injected into the aggregate).
    /// </summary>
    public bool IsExpired(DateTime now) => ExpiresAt < now;

    /// <summary>
    ///     Whether the token can still be consumed: not already consumed and not expired.
    /// </summary>
    public bool IsUsable(DateTime now) => !Consumed && !IsExpired(now);

    /// <summary>
    ///     Consumes this token (REQ-EV-2). Self-guarded — fails with
    ///     <see cref="IamErrors.VerificationTokenExpiredOrConsumed" /> when
    ///     already consumed or expired (collapses both cases into one 400,
    ///     matching OS's single combined response).
    /// </summary>
    public Result<Unit, Error> Consume(DateTime now)
    {
        if (!IsUsable(now))
            return new Result<Unit, Error>.Failure(IamErrors.VerificationTokenExpiredOrConsumed);

        Consumed = true;
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
