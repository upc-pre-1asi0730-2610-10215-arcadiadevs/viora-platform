namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

/// <summary>
///     Result of <c>IPaymentGateway.CreateCheckoutAsync</c> (REQ-GATE-1). A
///     read-model record — never persisted. <see cref="RedirectUrl" /> is
///     the gateway-hosted checkout page the client is sent to;
///     <see cref="ExternalReference" /> echoes back the reference used to
///     reconcile the eventual webhook (REQ-GATE-5).
/// </summary>
public record CheckoutSession(string RedirectUrl, string ExternalReference);