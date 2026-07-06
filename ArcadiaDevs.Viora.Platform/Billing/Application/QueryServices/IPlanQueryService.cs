using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;

/// <summary>
///     Service that handles Plan read queries (REQ-PLAN-2). Returns
///     <see cref="Result{TValue, TError}" /> (REQ-CC-3) so
///     <c>PlansController</c> can map failures via
///     <c>BillingActionResultAssembler</c>, matching the majority
///     Result-wrapped convention used by sibling BC query endpoints.
/// </summary>
public interface IPlanQueryService
{
    /// <summary>Lists the full seeded Plan catalog.</summary>
    Task<Result<IEnumerable<Plan>, Error>> Handle(GetAllPlansQuery query, CancellationToken cancellationToken = default);
}