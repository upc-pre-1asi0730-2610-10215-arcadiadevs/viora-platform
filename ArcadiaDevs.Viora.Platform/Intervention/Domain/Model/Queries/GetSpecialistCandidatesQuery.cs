namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for ranked specialist candidates for an alert (REQ-SPEC-3).
/// </summary>
public record GetSpecialistCandidatesQuery(long? AlertId, int Limit);
