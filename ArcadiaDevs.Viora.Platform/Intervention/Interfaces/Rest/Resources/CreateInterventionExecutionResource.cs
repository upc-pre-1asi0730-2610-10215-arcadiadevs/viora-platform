namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for certifying an InterventionExecution (REQ-IE-1).
/// </summary>
public record CreateInterventionExecutionResource(
    int TreatmentPrescriptionId,
    DateOnly ApplicationDate,
    decimal AppliedArea,
    string ExecutionStatus,
    string? FieldNote);
