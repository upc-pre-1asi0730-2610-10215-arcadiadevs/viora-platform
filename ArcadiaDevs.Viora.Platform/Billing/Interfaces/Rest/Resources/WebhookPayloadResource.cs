namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

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