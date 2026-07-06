using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;

/// <summary>
///     The Invoice aggregate root — a billing record created exclusively by
///     payment-webhook reconciliation (REQ-INV-1..4). <see cref="UserId" />,
///     <see cref="IssuedAt" />, <see cref="Description" />, <see cref="Amount" />
///     and <see cref="Currency" /> are ctor-only immutable.
/// </summary>
/// <remarks>
///     <see cref="Status" /> starts at <c>PENDING</c> and self-guards its only
///     two transitions (mirrors <see cref="Subscription" />'s established
///     <see cref="Result{TValue, TError}" />-returning convention):
///     <see cref="MarkPaid" />/<see cref="MarkFailed" />, both succeeding only
///     from <c>PENDING</c> (design's Per-Aggregate Design table). BUT — per
///     that same table — the constructor and the MarkPaid/MarkFailed call are
///     ALWAYS invoked together on the SAME in-memory instance before the
///     repository ever persists it (<c>InvoiceCommandService.Handle
///     (CreateInvoiceCommand)</c> constructs, immediately transitions, THEN
///     calls <c>AddAsync</c>). No truly "pending, unresolved" row is ever
///     written to the database — this preserves OS's exact behavioral
///     guarantee (REQ-INV-1: no invoice for an abandoned checkout) while
///     still exercising the self-guard convention used everywhere else in
///     this port.
/// </remarks>
public class Invoice
{
    public int Id { get; }

    public int UserId { get; }

    public DateTimeOffset IssuedAt { get; }

    public string Description { get; }

    public decimal Amount { get; }

    /// <summary>
    ///     ISO currency code. Defaults to <c>PEN</c> as a ctor default
    ///     parameter (REQ-CC-4), mirroring <c>Plan.Currency</c>/
    ///     <c>Subscription</c>'s pattern.
    /// </summary>
    public string Currency { get; }

    public InvoiceStatus Status { get; private set; }

    /// <summary>
    ///     MercadoPago's payment identifier. Nullable — only ever set by
    ///     <see cref="MarkPaid" />; a <c>FAILED</c> invoice never carries one.
    ///     DB-unique when present (REQ-INV-1 — duplicate webhook deliveries
    ///     for the same payment cannot double-invoice).
    /// </summary>
    public string? ExternalPaymentId { get; private set; }

    private Invoice()
    {
        Description = string.Empty;
        Currency = "PEN";
    }

    public Invoice(
        int userId,
        DateTimeOffset issuedAt,
        string description,
        decimal amount,
        string currency = "PEN")
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be positive.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        UserId = userId;
        IssuedAt = issuedAt;
        Description = description;
        Amount = amount;
        Currency = currency;
        Status = InvoiceStatus.PENDING;
    }

    /// <summary>
    ///     Marks the invoice as paid (REQ-INV-2). Self-guarded — only
    ///     succeeds from <c>PENDING</c>; already-terminal (<c>PAID</c> or
    ///     <c>FAILED</c>) returns a <see cref="BillingErrors.ConflictError" />
    ///     (409).
    /// </summary>
    public Result<Unit, Error> MarkPaid(string externalPaymentId)
    {
        if (Status != InvoiceStatus.PENDING)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.ConflictError);
        }

        if (string.IsNullOrWhiteSpace(externalPaymentId))
        {
            throw new ArgumentException("External payment ID is required.", nameof(externalPaymentId));
        }

        Status = InvoiceStatus.PAID;
        ExternalPaymentId = externalPaymentId;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Marks the invoice as failed (REQ-INV-2). Self-guarded — only
    ///     succeeds from <c>PENDING</c>; already-terminal (<c>PAID</c> or
    ///     <c>FAILED</c>) returns a <see cref="BillingErrors.ConflictError" />
    ///     (409).
    /// </summary>
    public Result<Unit, Error> MarkFailed()
    {
        if (Status != InvoiceStatus.PENDING)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.ConflictError);
        }

        Status = InvoiceStatus.FAILED;
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
