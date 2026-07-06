namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     Subscription resource (REQ-SUB-4).
/// </summary>
public record SubscriptionResource(
    int Id,
    int UserId,
    string PlanCode,
    string Interval,
    string Status,
    DateTimeOffset CurrentPeriodEnd);