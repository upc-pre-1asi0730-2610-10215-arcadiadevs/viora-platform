namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record CreateInterventionRequestResource(
    int GrowerId,
    long PlotId,
    int SpecialistId,
    long? AlertId,
    string Reason,
    string Message);
