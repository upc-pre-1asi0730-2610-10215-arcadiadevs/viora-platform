namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for <c>POST /api/v1/alerts/{id}/dismiss</c> (SURV-003).
///     Entirely optional; omit the request body to dismiss without a
///     caller-supplied reason (REQ-5).
/// </summary>
/// <param name="Reason">Optional caller-supplied dismissal reason.</param>
public record DismissAlertResource(
    string? Reason = null
);
