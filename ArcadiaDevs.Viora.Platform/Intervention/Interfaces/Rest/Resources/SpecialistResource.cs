namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Public specialist profile resource (REQ-SPEC-1) — no contact fields.
/// </summary>
public record SpecialistResource(
    int Id,
    string FullName,
    string Role,
    double? SuccessRate,
    int CaseCount,
    double? DistanceKm,
    IReadOnlyList<string> Tags,
    string Availability,
    string? PhotoUrl);
