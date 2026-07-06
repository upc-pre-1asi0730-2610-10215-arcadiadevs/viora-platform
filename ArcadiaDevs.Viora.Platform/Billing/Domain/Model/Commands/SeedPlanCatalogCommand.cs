namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to trigger the idempotent seeding of the demo Plan catalog
///     into the database (REQ-PLAN-1). Mirrors Intervention's
///     <c>SeedSpecialistsCommand</c> pattern.
/// </summary>
public record SeedPlanCatalogCommand();