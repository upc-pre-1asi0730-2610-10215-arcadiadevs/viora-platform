namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     TreatmentPrescription read resource (REQ-TP-1..4). The inspection and
///     prescription fields are <c>null</c> until <see cref="Status" />
///     reaches the corresponding stage.
/// </summary>
public record TreatmentPrescriptionResource(
    int Id,
    int ServiceProposalId,
    string Status,
    string? FindingType,
    string? IncidenceLevel,
    string? TechnicalDescription,
    DateOnly? RecordDate,
    string? ApplicationMethod,
    string? SprayVolume,
    string? PreHarvestInterval,
    string? AgronomistRecommendations,
    string? RequiredPPE,
    IReadOnlyList<string>? Products);
