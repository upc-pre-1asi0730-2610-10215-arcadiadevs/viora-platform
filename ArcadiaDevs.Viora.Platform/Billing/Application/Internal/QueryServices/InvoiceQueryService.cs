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
///     Handles Invoice read queries (REQ-INV-3), mapping failures through
///     <see cref="Result{TValue, TError}" /> (REQ-CC-3).
/// </summary>
/// <remarks>
///     Unlike <c>PaymentMethodQueryService</c>, this class DOES inject
///     <see cref="IIamContextFacade" /> — REQ-INV-3 explicitly cross-references
///     REQ-CC-2 (userId is direct client input on this read endpoint),
///     mirroring <c>SubscriptionQueryService</c>'s validate-then-lookup shape.
/// </remarks>
public class InvoiceQueryService(
    IInvoiceRepository invoiceRepository,
    IIamContextFacade iamContextFacade)
    : IInvoiceQueryService
{
    public async Task<Result<IEnumerable<Invoice>, Error>> Handle(
        GetInvoicesByUserIdQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // REQ-CC-2: unknown userId (IAM lookup fails) -> 404.
            if (!await iamContextFacade.ExistsUserAsync(query.UserId, cancellationToken))
            {
                return new Result<IEnumerable<Invoice>, Error>.Failure(BillingErrors.NotFound);
            }

            var invoices = await invoiceRepository.ListByUserIdAsync(query.UserId, cancellationToken);
            return new Result<IEnumerable<Invoice>, Error>.Success(invoices);
        }
        catch (OperationCanceledException)
        {
            return new Result<IEnumerable<Invoice>, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<IEnumerable<Invoice>, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<IEnumerable<Invoice>, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}
