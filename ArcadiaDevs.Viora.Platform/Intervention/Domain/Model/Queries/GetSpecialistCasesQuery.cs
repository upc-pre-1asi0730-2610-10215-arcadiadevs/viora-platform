namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for the specialist's own cases read model (My Requests + Field
///     Inspection), scoped to the signed-in specialist (derived from the
///     bearer token).
/// </summary>
/// <param name="SpecialistId">The authenticated caller's id, derived from the token.</param>
public record GetSpecialistCasesQuery(int SpecialistId);
