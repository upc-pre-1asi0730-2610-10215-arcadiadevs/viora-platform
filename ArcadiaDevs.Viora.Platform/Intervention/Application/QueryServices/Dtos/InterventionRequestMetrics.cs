namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;

/// <summary>
///     Aggregate <c>InterventionRequest</c> metrics (REQ-OV-3), scoped to a
///     single grower or specialist. Exact metric set was left to design's
///     discretion by the spec ("exact metric set deferred to design"); this
///     inferred set covers per-status counts plus two derived rates:
///     <see cref="AcceptanceRate" /> = <c>AcceptedCount / TotalRequests</c>
///     (share of all requests that reached <c>ACCEPTED</c>), and
///     <see cref="CompletionRate" /> = <c>ClosedCount / TotalRequests</c>
///     (share of all requests whose downstream chain reached the REQ-OV-2
///     <c>CLOSED</c> status). Both are <c>0</c> when <see cref="TotalRequests" />
///     is <c>0</c> (no division by zero).
/// </summary>
public record InterventionRequestMetrics(
    int TotalRequests,
    int PendingCount,
    int AwaitingResponseCount,
    int ProposalReceivedCount,
    int AcceptedCount,
    int DeclinedCount,
    double AcceptanceRate,
    int ClosedCount,
    double CompletionRate);
