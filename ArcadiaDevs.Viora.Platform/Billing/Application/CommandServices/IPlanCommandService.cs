using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;

/// <summary>
///     Service that handles commands related to the Plan catalog.
/// </summary>
public interface IPlanCommandService
{
    /// <summary>
    ///     Handles the idempotent seeding of the demo Plan catalog into the
    ///     database (REQ-PLAN-1).
    /// </summary>
    /// <param name="command">The command to seed the plan catalog.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Handle(SeedPlanCatalogCommand command, CancellationToken cancellationToken = default);
}