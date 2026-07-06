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
///     Handles PaymentMethod read queries (REQ-PM-3), mapping failures
///     through <see cref="Result{TValue, TError}" /> (REQ-CC-3).
/// </summary>
/// <remarks>
///     Unlike <c>SubscriptionQueryService</c>/the future Invoice query
///     service, this class does NOT inject <c>IIamContextFacade</c> — the
///     spec's cross-cutting REQ-CC-2 clause only enumerates "Subscription
///     creation/lookup, Coupon redemption, ReferralCode get-or-create" as
///     requiring IAM validation, and REQ-PM-3 (unlike REQ-INV-3) never
///     cross-references REQ-CC-2 either. This is a deliberate, spec-locked
///     asymmetry with Invoice's read endpoint (flagged explicitly so it does
///     not read as an inconsistency bug), not a narrowing decision made
///     during this slice's implementation.
/// </remarks>
public class PaymentMethodQueryService(IPaymentMethodRepository paymentMethodRepository) : IPaymentMethodQueryService
{
    public async Task<Result<IEnumerable<PaymentMethod>, Error>> Handle(
        GetPaymentMethodsByUserIdQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // REQ-PM-2's unique index on UserId guarantees at most one row;
            // the "list" shape (REQ-PM-3) is realized by wrapping that
            // single lookup as an at-most-one-element sequence rather than
            // adding a redundant dedicated list-repository method.
            var paymentMethod = await paymentMethodRepository.FindByUserIdAsync(query.UserId, cancellationToken);
            var paymentMethods = paymentMethod is null
                ? Enumerable.Empty<PaymentMethod>()
                : [paymentMethod];

            return new Result<IEnumerable<PaymentMethod>, Error>.Success(paymentMethods);
        }
        catch (OperationCanceledException)
        {
            return new Result<IEnumerable<PaymentMethod>, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<IEnumerable<PaymentMethod>, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<IEnumerable<PaymentMethod>, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}
