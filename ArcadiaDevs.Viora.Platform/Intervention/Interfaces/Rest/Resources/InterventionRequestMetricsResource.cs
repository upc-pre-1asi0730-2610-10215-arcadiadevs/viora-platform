namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

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
