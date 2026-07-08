namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

public record PaymentMethodResource(
    int Id,
    int UserId,
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear,
    bool IsDefault);