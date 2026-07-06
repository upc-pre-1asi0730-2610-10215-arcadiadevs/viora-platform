using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="Invoice" /> commands (REQ-INV-1, REQ-INV-2).
/// </summary>
/// <remarks>
///     Design's Per-Aggregate Design table: constructs the <see cref="Invoice" />
///     THEN immediately calls <see cref="Invoice.MarkPaid" />/
///     <see cref="Invoice.MarkFailed" /> on that SAME in-memory instance,
///     BEFORE ever calling <c>AddAsync</c> — no truly "pending, unresolved"
///     row is ever persisted, preserving OS's exact behavioral guarantee
///     (REQ-INV-1) while still exercising the self-guard convention.
/// </remarks>
public class InvoiceCommandService(
    IInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork)
    : IInvoiceCommandService
{
    public async Task<Result<Invoice, Error>> Handle(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // REQ-INV-1: pre-insert duplicate guard beyond the external_payment_id
            // unique-index DB constraint — mirrors WU1/WU2's ExistsByCodeAsync/
            // FindByUserIdAsync idempotency-guard idiom, so a replayed webhook
            // delivery maps to a clean 409 instead of an unhandled DbUpdateException.
            if (!string.IsNullOrWhiteSpace(command.ExternalPaymentId))
            {
                var existing = await invoiceRepository.FindByExternalPaymentIdAsync(command.ExternalPaymentId, cancellationToken);
                if (existing is not null)
                {
                    return new Result<Invoice, Error>.Failure(BillingErrors.ConflictError);
                }
            }

            var invoice = new Invoice(
                command.UserId,
                command.IssuedAt,
                command.Description,
                command.Amount,
                command.Currency);

            // ExternalPaymentId doubles as the paid/failed discriminator — see
            // CreateInvoiceCommand's remarks.
            var transitionResult = string.IsNullOrWhiteSpace(command.ExternalPaymentId)
                ? invoice.MarkFailed()
                : invoice.MarkPaid(command.ExternalPaymentId);

            if (transitionResult is Result<Unit, Error>.Failure transitionFailure)
            {
                return new Result<Invoice, Error>.Failure(transitionFailure.Error);
            }

            await invoiceRepository.AddAsync(invoice, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<Invoice, Error>.Success(invoice);
        }
        catch (ArgumentException)
        {
            return new Result<Invoice, Error>.Failure(BillingErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<Invoice, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<Invoice, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<Invoice, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}
