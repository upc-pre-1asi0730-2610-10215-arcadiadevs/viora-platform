namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

/// <summary>
///     Billing cadence for a <see cref="Aggregates.Plan" /> (REQ-PLAN-2).
/// </summary>
public enum PlanInterval
{
    MONTHLY,
    ANNUAL
}