namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;

/// <summary>
///     Query to list every invoice belonging to a user (REQ-INV-3).
///     <c>UserId</c> is direct client input on this read endpoint and MUST
///     be validated via <c>IIamContextFacade</c> before the repository
///     lookup (REQ-CC-2) — unlike PaymentMethod's list-read, which is
///     spec-exempt.
/// </summary>
public record GetInvoicesByUserIdQuery(int UserId);