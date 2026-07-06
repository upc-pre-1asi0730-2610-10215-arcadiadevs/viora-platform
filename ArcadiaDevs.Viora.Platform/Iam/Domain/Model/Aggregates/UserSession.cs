namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;

/// <summary>
///     The user-session aggregate — captures a device/browser's
///     <c>User-Agent</c> for a successful sign-in (REQ-SESS-1). Simplified per
///     the proposal's literal scope vs. OS's <c>UserSession</c> — no
///     Device/Client/Location parsing (no UA-parsing dependency exists in WA;
///     deliberate scope simplification).
/// </summary>
public partial class UserSession(int userId, string userAgent, DateTime lastActiveAt, bool isCurrent)
{
    public UserSession() : this(0, string.Empty, DateTime.MinValue, false)
    {
    }

    public int Id { get; }
    public int UserId { get; private set; } = userId;

    /// <summary>The raw User-Agent header value, "Unknown" if absent.</summary>
    public string UserAgent { get; private set; } = userAgent;

    /// <summary>
    ///     Domain field, decoupled from <c>IAuditableEntity.UpdatedAt</c> (audit
    ///     metadata is infra, not read by domain logic).
    /// </summary>
    public DateTime LastActiveAt { get; private set; } = lastActiveAt;

    public bool IsCurrent { get; private set; } = isCurrent;

    /// <summary>
    ///     Demotes this session from being the current one — invoked on every
    ///     new sign-in for the same user before inserting a fresh row
    ///     (REQ-SESS-4, no dedup, every sign-in creates a new row).
    /// </summary>
    public void Demote() => IsCurrent = false;
}
