namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     InterventionExecution read resource (REQ-IE-1..3).
/// </summary>
public record InterventionExecutionResource(
    int Id,
    int TreatmentPrescriptionId,
    DateOnly ApplicationDate,
    decimal AppliedArea,
    string ExecutionStatus,
    string? FieldNote);
