using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to switch a user's subscription to a different plan
///     (REQ-SUB-3). Internal-only — no controller route in this or any WU2
///     slice; invoked later from WU6's payment-webhook reconciliation only.
///     Self-guarded on the aggregate (<c>ACTIVE</c>-only; a <c>CANCELED</c>
///     subscription must reactivate via a fresh checkout instead) and
///     validates <paramref name="PlanCode" /> exists in the Plan catalog
///     (404 if unknown) at the command-service layer.
/// </summary>
public record SwitchPlanCommand(int UserId, string PlanCode, PlanInterval Interval);