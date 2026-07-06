using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to create a checkout session for a user and target plan
///     (REQ-GATE-3). Handled by <c>CheckoutCommandService</c>, which
///     short-circuits to <c>BillingErrors.PaymentGatewayNotConfigured</c>
///     (mapped 503, not an unhandled exception) when
///     <c>IPaymentGateway.IsConfigured</c> is <c>false</c>.
/// </summary>
public record CreateCheckoutCommand(int UserId, string PlanCode, PlanInterval Interval);