using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;

/// <summary>
///     Static domain error codes for the Billing bounded context. Shared by
///     all 6 aggregates (Plan, Subscription, PaymentMethod, Invoice, Coupon,
///     ReferralCode — REQ-CC-3), mirroring <c>InterventionErrors</c>. WU1
///     seeds the base set; WU5 adds <c>PaymentGatewayNotConfigured</c> (503)
///     once the payment-gateway port lands.
/// </summary>
public static class BillingErrors
{
    public static readonly Error NotFound =
        new("Billing.NotFound", "The specified billing resource was not found.");

    public static readonly Error ValidationError =
        new("Billing.ValidationError", "The request failed validation.");

    public static readonly Error ConflictError =
        new("Billing.ConflictError", "The operation conflicts with the current state of the resource.");

    public static readonly Error DatabaseError =
        new("Billing.DatabaseError", "A database error occurred while processing the billing operation.");

    public static readonly Error InternalServerError =
        new("Billing.InternalServerError", "An unexpected internal error occurred in Billing.");

    public static readonly Error OperationCancelled =
        new("Billing.OperationCancelled", "The operation was cancelled.");

    /// <summary>
    ///     The payment gateway adapter is disabled or missing its access
    ///     token (REQ-GATE-2, REQ-GATE-3). Maps to 503
    ///     (<c>BillingActionResultAssembler</c>) — the first Billing error
    ///     needing that status. Added in WU5 (payment-gateway-port).
    /// </summary>
    public static readonly Error PaymentGatewayNotConfigured =
        new("Billing.PaymentGatewayNotConfigured", "The payment gateway is not configured.");
}