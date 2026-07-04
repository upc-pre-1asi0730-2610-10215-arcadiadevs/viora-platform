namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Aggregate request-metrics resource (REQ-OV-3), scoped to a single
///     grower or specialist.
/// </summary>
public record InterventionRequestMetricsResource(
    int TotalRequests,
    int PendingCount,
    int AwaitingResponseCount,
    int ProposalReceivedCount,
    int AcceptedCount,
    int DeclinedCount,
    double AcceptanceRate,
    int ClosedCount,
    double CompletionRate);
