namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;

/// <summary>
///     Public specialist profile (REQ-SPEC-1) — composed at read time
///     entirely from the referenced Profile (Role=Specialist) via
///     <c>IProfileContextFacade</c>, plus a live-computed
///     <see cref="CaseCount" />. Deliberately excludes
///     <c>phone</c>/<c>email</c>/<c>whatsapp</c>.
/// </summary>
/// <remarks>
///     <see cref="SuccessRate" /> is null until a real closed-case
///     derivation exists (deferred to a later phase — do not fabricate a
///     value). <see cref="DistanceKm" /> is null whenever there's no alert
///     context to measure against, the specialist has no geolocation set,
///     or the distance lookup itself returns null — never a fabricated 0.
/// </remarks>
public record SpecialistPublicProfile(
    int Id,
    string FullName,
    string Role,
    double? SuccessRate,
    int CaseCount,
    double? DistanceKm,
    IReadOnlyList<string> Tags,
    string Availability);
