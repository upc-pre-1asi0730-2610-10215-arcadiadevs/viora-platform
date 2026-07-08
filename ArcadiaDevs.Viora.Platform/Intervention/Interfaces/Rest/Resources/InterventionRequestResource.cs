namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record InterventionRequestResource(
    int Id,
    int GrowerId,
    long PlotId,
    int SpecialistId,
    long? AlertId,
    string Reason,
    string Message,
    string Status,
    string? DeclineReason);
