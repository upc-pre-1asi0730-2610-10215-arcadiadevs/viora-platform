namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

public record UpdateAlertResource(
    string? Status,
    string? Reason = null,
    bool RaiseSeverity = false
);
