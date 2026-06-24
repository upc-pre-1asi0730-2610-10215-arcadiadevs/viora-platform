namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for partially updating an alert. Currently only the status
///     transition to <c>UNDER_REVIEW</c> is supported.
/// </summary>
/// <param name="Status">The target status (e.g. <c>UNDER_REVIEW</c>).</param>
public record UpdateAlertResource(
    string Status
);
