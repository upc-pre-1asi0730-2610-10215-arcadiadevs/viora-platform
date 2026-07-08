namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record SpecialistCasesResource(
    int AwaitingResponseCount,
    int InProgressCount,
    int ClosedCount,
    int DeclinedCount,
    int NeedsVisitCount,
    int PrescriptionPendingCount,
    int PrescribedCount,
    double? AcceptanceRatePercent,
    IReadOnlyList<SpecialistCasesResource.Case> Cases,
    string UpdatedAt)
{
    public record Case(
        int RequestId,
        string ReferenceCode,
        int? ServiceProposalId,
        int? TreatmentPrescriptionId,
        string RequestStatus,
        string? FieldStage,
        string? Severity,
        string? Problem,
        string? ProducerName,
        string? PlotName,
        string? Location,
        decimal? Amount,
        string? Currency,
        string? ProposedDate,
        string? CreatedAt,
        string? UpdatedAt);
}
