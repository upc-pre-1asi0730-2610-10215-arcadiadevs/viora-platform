using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.QueryServices;

/// <summary>
///     Handles Coupon read queries (REQ-COUP-4), mapping failures through
///     <see cref="Result{TValue, TError}" /> (REQ-CC-3).
/// </summary>
/// <remarks>
///     Injects <see cref="IIamContextFacade" /> — <c>userId</c> is direct
///     client input on this read endpoint (REQ-CC-2), mirroring
///     <c>InvoiceQueryService</c>'s validate-then-lookup shape.
/// </remarks>
public class CouponQueryService(
    ICouponRepository couponRepository,
    IIamContextFacade iamContextFacade)
    : ICouponQueryService
{
    public async Task<Result<IEnumerable<Coupon>, Error>> Handle(
        GetCouponsByUserIdQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await iamContextFacade.ExistsUserAsync(query.UserId, cancellationToken))
            {
                return new Result<IEnumerable<Coupon>, Error>.Failure(BillingErrors.NotFound);
            }

            var coupons = await couponRepository.ListByUserIdAsync(query.UserId, cancellationToken);
            return new Result<IEnumerable<Coupon>, Error>.Success(coupons);
        }
        catch (OperationCanceledException)
        {
            return new Result<IEnumerable<Coupon>, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<IEnumerable<Coupon>, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<IEnumerable<Coupon>, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}
