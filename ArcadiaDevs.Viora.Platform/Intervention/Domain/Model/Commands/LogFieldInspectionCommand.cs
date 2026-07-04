namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to log the field inspection of a
///     <see cref="Aggregates.TreatmentPrescription" /> (REQ-TP-2).
///     Self-guarded on the aggregate — only succeeds from
///     <c>PENDING_INSPECTION</c> (409 otherwise).
/// </summary>
public record LogFieldInspectionCommand(
    int Id,
    string? FindingType,
    string? IncidenceLevel,
    string? TechnicalDescription,
    DateOnly? RecordDate);
