namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

public record CheckoutSessionResource(string CheckoutUrl, string PreferenceId, string ExternalReference);