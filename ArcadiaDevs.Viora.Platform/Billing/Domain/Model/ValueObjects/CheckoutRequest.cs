namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

/// <summary>
///     Input to <c>IPaymentGateway.CreateCheckoutAsync</c> (REQ-GATE-1). A
///     read-model record — never persisted. <see cref="ExternalReference" />
///     is built by <c>CheckoutCommandService</c> as
///     <c>"{userId}:{planCode}:{interval}"</c> (design's Webhook flow
///     section) and threaded through to WU6's webhook reconciliation.
/// </summary>
public record CheckoutRequest(
    int UserId,
    string PlanCode,
    PlanInterval Interval,
    decimal Amount,
    string Currency,
    string ExternalReference);