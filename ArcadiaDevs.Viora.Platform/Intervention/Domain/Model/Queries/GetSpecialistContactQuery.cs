namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for a specialist's gated contact info (REQ-SPEC-2). Contact is
///     only unlocked when the referenced <c>InterventionRequest</c> is
///     <c>ACCEPTED</c> and matches <paramref name="SpecialistId" />.
/// </summary>
public record GetSpecialistContactQuery(int SpecialistId, int RequestId);
