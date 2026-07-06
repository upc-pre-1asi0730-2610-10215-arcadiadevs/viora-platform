namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     PaymentMethod resource (REQ-PM-3). Display metadata only — never the
///     full card number or CVV (REQ-PM-1).
/// </summary>
public record PaymentMethodResource(
    int Id,
    int UserId,
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear,
    bool IsDefault);