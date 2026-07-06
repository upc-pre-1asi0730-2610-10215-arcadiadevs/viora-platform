namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;

/// <summary>
///     Query to list the payment method(s) belonging to a user (REQ-PM-3).
///     Shaped as a list read for REST/OS parity even though the unique
///     index on <c>UserId</c> (REQ-PM-2) means at most one row can ever
///     exist per user.
/// </summary>
public record GetPaymentMethodsByUserIdQuery(int UserId);