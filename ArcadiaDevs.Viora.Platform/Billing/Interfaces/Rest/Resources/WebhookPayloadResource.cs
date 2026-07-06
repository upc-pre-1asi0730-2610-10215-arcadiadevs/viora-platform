namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     POST payload for <c>/webhooks/mercado-pago</c> (REQ-GATE-4). Accepts
///     MercadoPago's real minimal notification shape (<see cref="Id" />,
///     <see cref="Type" />) PLUS an OPTIONAL inline synthetic payload used
///     ONLY when the payment gateway adapter is unconfigured (design's
///     demoability-without-live-credentials mechanism):
///     <see cref="PaymentId" />/<see cref="ExternalReference" />/
///     <see cref="Status" />/<see cref="Amount" />/<see cref="Currency" />
///     mirror <c>PaymentInfo</c>'s shape directly, so the reconciliation flow
///     (the actual business value under test) stays demoable with zero real
///     credentials.
/// </summary>
/// <remarks>
///     <see cref="CardBrand" />/<see cref="CardLast4" />/
///     <see cref="CardExpMonth" />/<see cref="CardExpYear" /> are an
///     ADDITIONAL optional group beyond the base inline-field list — without
///     them, REQ-GATE-5's "upsert PaymentMethod if card data present" side
///     effect would be permanently unreachable dead code in the demoability
///     path, since neither MercadoPago's real minimal shape nor the base
///     inline fields ever carry card metadata (see <c>PaymentInfo</c>'s
///     matching remarks). Present only in the unconfigured/demoability path.
/// </remarks>
public record WebhookPayloadResource(
    string? Id,
    string? Type,
    string? PaymentId,
    string? ExternalReference,
    string? Status,
    decimal? Amount,
    string? Currency,
    string? CardBrand = null,
    string? CardLast4 = null,
    int? CardExpMonth = null,
    int? CardExpYear = null);