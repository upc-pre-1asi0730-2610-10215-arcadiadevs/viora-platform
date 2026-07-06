namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

/// <summary>
///     Result of <c>IPaymentGateway.FetchPaymentAsync</c> (REQ-GATE-1). A
///     read-model record — never persisted. Consumed by WU6's webhook
///     reconciliation to decide Invoice/Subscription/PaymentMethod side
///     effects (REQ-GATE-5).
/// </summary>
/// <remarks>
///     <see cref="CardBrand" />/<see cref="CardLast4" />/
///     <see cref="CardExpMonth" />/<see cref="CardExpYear" /> are trailing
///     optional fields added in WU6 (default <c>null</c>, backward-compatible
///     with WU5's existing adapter constructor call site) — REQ-GATE-5/REQ-
///     PM-2 require the reconciliation flow to "upsert PaymentMethod if card
///     data present"; without these fields that side effect would be
///     permanently unreachable dead code, since no wire shape in this port
///     otherwise carries card metadata. The real MercadoPago adapter
///     (WU5) does not populate them yet (stub response mapping, deferred
///     per spec's "Design-deferred items" #1) — they are populated by WU6's
///     webhook controller only via the inline synthetic/demoability payload.
/// </remarks>
public record PaymentInfo(
    string PaymentId,
    string Status,
    decimal Amount,
    string Currency,
    string ExternalReference,
    string? CardBrand = null,
    string? CardLast4 = null,
    int? CardExpMonth = null,
    int? CardExpYear = null);