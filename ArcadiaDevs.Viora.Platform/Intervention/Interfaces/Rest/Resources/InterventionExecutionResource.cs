namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record InterventionExecutionResource(
    int Id,
    int TreatmentPrescriptionId,
    DateOnly ApplicationDate,
    decimal AppliedArea,
    string ExecutionStatus,
    string? FieldNote);
