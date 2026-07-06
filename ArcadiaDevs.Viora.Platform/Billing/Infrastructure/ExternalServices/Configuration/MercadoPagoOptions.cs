namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.ExternalServices.Configuration;

/// <summary>
///     Strongly-typed options for the MercadoPago payment-gateway adapter
///     (REQ-GATE-2). Bound from configuration section
///     <see cref="SectionName" />. Validated at startup by
///     <see cref="MercadoPagoOptionsValidator" /> — but unlike the
///     <c>AgroMonitoringWeatherOptionsValidator</c> precedent it composes
///     from, validation only requires <see cref="AccessToken" /> when
///     <see cref="Enabled" /> is <c>true</c> (deliberate off-switch
///     divergence — see the validator's remarks).
/// </summary>
public class MercadoPagoOptions
{
    /// <summary>
    ///     The configuration section path used for binding.
    /// </summary>
    public const string SectionName = "Billing:PaymentGateway:MercadoPago";

    /// <summary>
    ///     Off by default (REQ-GATE-2) — the app MUST build/run with zero
    ///     real credentials until an operator explicitly turns this on.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     The MercadoPago sandbox/production access token. Required only
    ///     when <see cref="Enabled" /> is <c>true</c>.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    ///     The MercadoPago API base URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.mercadopago.com";
}