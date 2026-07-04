namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Immutable value object recording a specialist's field inspection of a
///     <see cref="Aggregates.TreatmentPrescription" /> (REQ-TP-2). Logged
///     exactly once, transitioning the prescription from
///     <c>PENDING_INSPECTION</c> to <c>INSPECTED</c>.
/// </summary>
public record FieldInspectionRecord
{
    public string FindingType { get; }

    public string IncidenceLevel { get; }

    public string TechnicalDescription { get; }

    public DateOnly RecordDate { get; }

    public FieldInspectionRecord(
        string findingType,
        string incidenceLevel,
        string technicalDescription,
        DateOnly recordDate)
    {
        if (string.IsNullOrWhiteSpace(findingType))
        {
            throw new ArgumentException("Finding type is required.", nameof(findingType));
        }

        if (string.IsNullOrWhiteSpace(incidenceLevel))
        {
            throw new ArgumentException("Incidence level is required.", nameof(incidenceLevel));
        }

        if (string.IsNullOrWhiteSpace(technicalDescription))
        {
            throw new ArgumentException("Technical description is required.", nameof(technicalDescription));
        }

        FindingType = findingType;
        IncidenceLevel = incidenceLevel;
        TechnicalDescription = technicalDescription;
        RecordDate = recordDate;
    }
}
