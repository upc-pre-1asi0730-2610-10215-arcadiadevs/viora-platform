namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to create a new <see cref="Aggregates.Invoice" /> for a user
///     (REQ-INV-1, REQ-INV-2). Internal-only — no public REST endpoint
///     invokes this command in this slice; it is wired from WU6's
///     payment-webhook reconciliation once a payment notification (approved
///     or not) is resolved.
/// </summary>
/// <remarks>
///     <c>ExternalPaymentId</c> doubles as the paid/failed discriminator:
///     the handler calls <c>Invoice.MarkPaid(ExternalPaymentId)</c> when a
///     non-blank value is supplied, or <c>Invoice.MarkFailed()</c> otherwise
///     — matching <c>Invoice.MarkFailed()</c>'s no-parameter signature
///     (design's Per-Aggregate Design table), under which a failed invoice
///     never carries an external payment reference. <c>UserId</c> here is
///     internally derived from an already-validated <c>Subscription</c>/
///     checkout flow, NOT direct client input — exempt from REQ-CC-2's
///     IAM-validation requirement per that REQ's own exemption clause.
/// </remarks>
public record CreateInvoiceCommand(
    int UserId,
    DateTimeOffset IssuedAt,
    string Description,
    decimal Amount,
    string? ExternalPaymentId,
    string Currency = "PEN");