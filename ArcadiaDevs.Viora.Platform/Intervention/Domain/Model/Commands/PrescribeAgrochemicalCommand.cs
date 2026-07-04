namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to issue the agrochemical prescription for a
///     <see cref="Aggregates.TreatmentPrescription" /> (REQ-TP-3).
///     Self-guarded on the aggregate — only succeeds from
///     <c>INSPECTED</c> (409 otherwise).
/// </summary>
public record PrescribeAgrochemicalCommand(
    int Id,
    string? ApplicationMethod,
    string? SprayVolume,
    string? PreHarvestInterval,
    string? AgronomistRecommendations,
    string? RequiredPPE,
    IReadOnlyList<string>? Products);
