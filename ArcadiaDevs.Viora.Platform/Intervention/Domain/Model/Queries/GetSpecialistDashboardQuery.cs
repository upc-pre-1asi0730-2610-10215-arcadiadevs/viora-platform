namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for the specialist segment dashboard read model, scoped to the
///     signed-in specialist.
/// </summary>
/// <param name="SpecialistId">The authenticated caller's id, derived from the token.</param>
public record GetSpecialistDashboardQuery(int SpecialistId);
