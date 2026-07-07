namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     Response body for a successfully created checkout session (REQ-GATE-3).
/// </summary>
public record CheckoutSessionResource(string CheckoutUrl, string ExternalReference);