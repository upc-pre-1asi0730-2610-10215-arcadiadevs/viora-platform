namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to reconcile a MercadoPago payment notification (REQ-GATE-4,
///     REQ-GATE-5). Carries BOTH MercadoPago's real minimal webhook shape
///     (<see cref="Id" />, <see cref="Type" />) and the optional inline
///     synthetic payload used only when the gateway is unconfigured — the
///     handler decides which to trust based on <c>IPaymentGateway.IsConfigured</c>,
///     never the caller.
/// </summary>
/// <remarks>
///     Internal-only — invoked exclusively from
///     <c>MercadoPagoWebhookController</c>, which always returns 200
///     regardless of this command's outcome (REQ-GATE-4).
/// </remarks>
public record ReconcilePaymentCommand(
    string? Id,
    string? Type,
    string? PaymentId,
    string? ExternalReference,
    string? Status,
    decimal? Amount,
    string? Currency,
    string? CardBrand,
    string? CardLast4,
    int? CardExpMonth,
    int? CardExpYear);