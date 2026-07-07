namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;

/// <summary>
///     Query for the specialist Intervention Marketplace read model, scoped
///     to the signed-in specialist (derived from the bearer token).
/// </summary>
/// <param name="SpecialistId">The authenticated caller's id, derived from the token.</param>
public record GetSpecialistMarketplaceQuery(int SpecialistId);
