namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to upsert a user's saved payment method display metadata
///     (REQ-PM-2). Internal-only — invoked from WU6's payment-webhook
///     reconciliation flow when an approved payment reveals card metadata;
///     no public REST endpoint accepts this command in this slice. Finds the
///     existing row by <c>UserId</c> and reuses it (ctor-replace) rather than
///     inserting a new row per payment.
/// </summary>
/// <remarks>
///     <c>UserId</c> here is internally derived from an already-validated
///     <c>Subscription</c>/checkout flow, NOT direct client input — exempt
///     from REQ-CC-2's IAM-validation requirement per that REQ's own
///     exemption clause ("Commands that derive userId internally from an
///     already-validated aggregate... are exempt — the FK was validated
///     upstream and MUST NOT be re-validated").
/// </remarks>
public record UpsertPaymentMethodCommand(
    int UserId,
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear,
    bool IsDefault);