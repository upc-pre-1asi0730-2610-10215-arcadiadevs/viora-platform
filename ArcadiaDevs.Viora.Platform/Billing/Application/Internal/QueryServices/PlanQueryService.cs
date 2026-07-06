using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.QueryServices;

/// <summary>
///     Handles Plan read queries (REQ-PLAN-2), mapping failures through
///     <see cref="Result{TValue, TError}" /> (REQ-CC-3) so
///     <c>PlansController</c> can route via <c>BillingActionResultAssembler</c>.
/// </summary>
public class PlanQueryService(IPlanRepository planRepository) : IPlanQueryService
{
    public async Task<Result<IEnumerable<Plan>, Error>> Handle(GetAllPlansQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var plans = await planRepository.ListAsync(cancellationToken);
            return new Result<IEnumerable<Plan>, Error>.Success(plans);
        }
        catch (OperationCanceledException)
        {
            return new Result<IEnumerable<Plan>, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<IEnumerable<Plan>, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<IEnumerable<Plan>, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}