using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to create a new <see cref="Aggregates.Subscription" /> for a
///     user (REQ-SUB-1). <c>UserId</c> is direct client input and MUST be
///     validated via <c>IIamContextFacade</c> before persisting (REQ-CC-2).
///     No public REST endpoint invokes this command in this slice — it is
///     wired internally from WU6's payment-webhook reconciliation (first
///     successful payment for a user with no existing subscription).
/// </summary>
public record CreateSubscriptionCommand(
    int UserId,
    string PlanCode,
    PlanInterval Interval,
    DateTimeOffset CurrentPeriodEnd);