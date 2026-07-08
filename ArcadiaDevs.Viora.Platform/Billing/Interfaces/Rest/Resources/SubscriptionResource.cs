namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

public record SubscriptionResource(
    int Id,
    int UserId,
    string PlanCode,
    string Interval,
    string Status,
    DateTimeOffset CurrentPeriodEnd);