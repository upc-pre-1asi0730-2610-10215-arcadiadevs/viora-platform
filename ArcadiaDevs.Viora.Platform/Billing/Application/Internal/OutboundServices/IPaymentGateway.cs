using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.OutboundServices;

/// <summary>
///     Outbound hexagonal port for the payment gateway (REQ-GATE-1),
///     independent of any concrete adapter. The sole concrete implementation
///     in this port is <c>MercadoPagoPaymentGatewayAdapter</c> (WU5), built
///     by composing two distinct existing precedents in this codebase: the
///     raw-<c>HttpClient</c>-registration shape of <c>AgroMonitoringApiClient</c>
///     plus the Options+Validator shape of
///     <c>AgroMonitoringWeatherDataService</c>/
///     <c>AgroMonitoringWeatherOptionsValidator</c> (design's PaymentGateway
///     Port Design section).
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    ///     <c>true</c> only when the adapter has a sandbox token explicitly
    ///     configured (REQ-GATE-2). <c>false</c> by default — the app MUST
    ///     build/run with zero real credentials.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    ///     Creates a checkout session for the given request (REQ-GATE-3).
    ///     Callers MUST check <see cref="IsConfigured" /> first — this
    ///     method does not implicitly gate itself.
    /// </summary>
    Task<Result<CheckoutSession, Error>> CreateCheckoutAsync(
        CheckoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Fetches payment details by the gateway's own payment identifier
    ///     (REQ-GATE-5) — consumed by WU6's webhook reconciliation when the
    ///     gateway is configured (the inline synthetic payload is used
    ///     instead when unconfigured, per design's demoability mechanism).
    /// </summary>
    Task<Result<PaymentInfo, Error>> FetchPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken = default);
}
