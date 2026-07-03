namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for a specialist's gated contact info (REQ-SPEC-2). Contact is
///     only unlocked when the referenced <c>InterventionRequest</c> is
///     <c>ACCEPTED</c>, matches <paramref name="SpecialistId" />, AND the
///     authenticated caller (<paramref name="CallerUserId" />) owns the
///     request (i.e. is its <c>GrowerId</c>) — status+specialist-id
///     matching alone is not sufficient to authorize the caller (WU1 fix
///     pass item #10, obs #272).
/// </summary>
public record GetSpecialistContactQuery(int SpecialistId, int RequestId, int CallerUserId);
