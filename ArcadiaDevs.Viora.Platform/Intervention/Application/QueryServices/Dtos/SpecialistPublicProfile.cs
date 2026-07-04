namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;

/// <summary>
///     Public specialist profile (REQ-SPEC-1) — composed at read time from
///     the <c>Specialist</c> aggregate (business fields) and the referenced
///     Profile (identity fields), via <c>IProfileContextFacade</c>.
///     Deliberately excludes <c>phone</c>/<c>email</c>/<c>whatsapp</c>.
/// </summary>
public record SpecialistPublicProfile(
    int Id,
    string FullName,
    string Role,
    double SuccessRate,
    int CaseCount,
    double DistanceKm,
    IReadOnlyList<string> Tags,
    string Availability);
