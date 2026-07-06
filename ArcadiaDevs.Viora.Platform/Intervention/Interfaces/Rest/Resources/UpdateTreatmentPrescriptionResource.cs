namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Resource DTO for partially updating a TreatmentPrescription (REQ-TP-2,
///     REQ-TP-3). The <paramref name="Stage" /> field selects which
///     self-guarded transition to apply: <c>INSPECTION</c> (
///     <c>PENDING_INSPECTION</c> → <c>INSPECTED</c>, REQ-TP-2) or
///     <c>PRESCRIPTION</c> (<c>INSPECTED</c> → <c>PRESCRIBED</c>, REQ-TP-3).
///     Only the fields relevant to the selected stage are read; the others
///     are ignored.
/// </summary>
/// <param name="Stage">The target stage. <c>INSPECTION</c> or <c>PRESCRIPTION</c>.</param>
/// <param name="FindingType">INSPECTION stage: the finding type.</param>
/// <param name="IncidenceLevel">INSPECTION stage: the incidence level.</param>
/// <param name="TechnicalDescription">INSPECTION stage: the technical description.</param>
/// <param name="RecordDate">INSPECTION stage: the inspection record date.</param>
/// <param name="ApplicationMethod">PRESCRIPTION stage: the application method (enum literal name).</param>
/// <param name="SprayVolumeAmount">PRESCRIPTION stage: the spray volume amount.</param>
/// <param name="SprayVolumeUnit">PRESCRIPTION stage: the spray volume unit.</param>
/// <param name="PreHarvestIntervalDays">PRESCRIPTION stage: the pre-harvest interval, in days.</param>
/// <param name="AgronomistRecommendations">PRESCRIPTION stage: the agronomist recommendations.</param>
/// <param name="RequiredPPE">PRESCRIPTION stage: the required personal protective equipment (enum literal names).</param>
/// <param name="Products">PRESCRIPTION stage: the prescribed agrochemical products.</param>
public record UpdateTreatmentPrescriptionResource(
    string? Stage,
    string? FindingType,
    string? IncidenceLevel,
    string? TechnicalDescription,
    DateOnly? RecordDate,
    string? ApplicationMethod,
    int? SprayVolumeAmount,
    string? SprayVolumeUnit,
    int? PreHarvestIntervalDays,
    string? AgronomistRecommendations,
    IReadOnlyList<string>? RequiredPPE,
    IReadOnlyList<PrescribedProductResource>? Products);
