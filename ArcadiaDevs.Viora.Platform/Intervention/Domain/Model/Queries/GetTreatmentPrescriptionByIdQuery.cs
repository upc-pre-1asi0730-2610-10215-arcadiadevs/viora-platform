namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a <see cref="Aggregates.TreatmentPrescription" />
///     by its id.
/// </summary>
public record GetTreatmentPrescriptionByIdQuery(int Id);
