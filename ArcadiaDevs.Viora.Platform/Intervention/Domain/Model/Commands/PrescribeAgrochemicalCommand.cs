namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to issue the agrochemical prescription for a
///     <see cref="Aggregates.TreatmentPrescription" /> (REQ-TP-3).
///     Self-guarded on the aggregate — only succeeds from
///     <c>INSPECTED</c> (409 otherwise).
/// </summary>
/// <param name="Id">The treatment prescription id.</param>
/// <param name="ApplicationMethod">The application method (enum literal name).</param>
/// <param name="SprayVolumeAmount">The spray volume amount.</param>
/// <param name="SprayVolumeUnit">The spray volume unit.</param>
/// <param name="PreHarvestIntervalDays">The pre-harvest interval, in days.</param>
/// <param name="AgronomistRecommendations">General agronomist recommendations.</param>
/// <param name="RequiredPPE">Required PPE (enum literal names).</param>
/// <param name="Products">The prescribed products.</param>
public record PrescribeAgrochemicalCommand(
    int Id,
    string? ApplicationMethod,
    int? SprayVolumeAmount,
    string? SprayVolumeUnit,
    int? PreHarvestIntervalDays,
    string? AgronomistRecommendations,
    IReadOnlyList<string>? RequiredPPE,
    IReadOnlyList<PrescribedProductCommandItem>? Products);

/// <summary>
///     A single prescribed product within a <see cref="PrescribeAgrochemicalCommand" />.
/// </summary>
public record PrescribedProductCommandItem(
    string ProductName,
    double DosageAmount,
    string DosageUnit,
    int SessionsCount,
    string? TechnicalRecommendation);
