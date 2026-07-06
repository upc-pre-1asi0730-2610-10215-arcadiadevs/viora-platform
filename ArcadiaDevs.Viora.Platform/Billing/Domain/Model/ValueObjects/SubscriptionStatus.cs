namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

/// <summary>
///     Lifecycle status of a <see cref="Aggregates.Subscription" /> (REQ-SUB-5).
/// </summary>
public enum SubscriptionStatus
{
    ACTIVE,
    PENDING,
    CANCELED
}