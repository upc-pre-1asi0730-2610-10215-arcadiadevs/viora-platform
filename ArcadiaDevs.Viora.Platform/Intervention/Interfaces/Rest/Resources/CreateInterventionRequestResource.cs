namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for creating an InterventionRequest (REQ-IREQ-1).
/// </summary>
public record CreateInterventionRequestResource(
    int GrowerId,
    long PlotId,
    int SpecialistId,
    long? AlertId,
    string Reason,
    string Message);
