namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for partially updating an alert. Supports transitioning
///     to <c>UNDER_REVIEW</c>, <c>RESOLVED</c>, or <c>DISMISSED</c>, and/or
///     raising the alert's severity by one level.
/// </summary>
/// <param name="Status">
///     The target status (<c>UNDER_REVIEW</c>, <c>RESOLVED</c>, or
///     <c>DISMISSED</c>). May be omitted when <paramref name="RaiseSeverity"/>
///     is <c>true</c>, to raise severity without changing status.
/// </param>
/// <param name="Reason">
///     Optional caller-supplied reason, used only when <see cref="Status"/> is
///     <c>DISMISSED</c> (REQ-5). Ignored for other target statuses.
/// </param>
/// <param name="RaiseSeverity">
///     When <c>true</c>, raises the alert's severity by one level. Combined
///     with <c>Status: "UNDER_REVIEW"</c> this confirms the alert from
///     inspection; with <see cref="Status"/> omitted, it escalates severity
///     only, with no status change.
/// </param>
public record UpdateAlertResource(
    string? Status,
    string? Reason = null,
    bool RaiseSeverity = false
);
