namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

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
