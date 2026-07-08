namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record CreateInterventionExecutionResource(
    int TreatmentPrescriptionId,
    DateOnly ApplicationDate,
    decimal AppliedArea,
    string ExecutionStatus,
    string? FieldNote);
