namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     Invoice resource (REQ-INV-3).
/// </summary>
public record InvoiceResource(
    int Id,
    int UserId,
    DateTimeOffset IssuedAt,
    string Description,
    decimal Amount,
    string Currency,
    string Status,
    string? ExternalPaymentId);