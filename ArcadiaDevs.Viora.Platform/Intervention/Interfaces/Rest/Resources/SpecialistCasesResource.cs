namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Read model for the specialist's own cases, powering both My Requests
///     (the pipeline grouped by request status) and Field Inspection
///     (accepted cases grouped by their on-site lifecycle stage). One
///     projection serves both screens; each filters/groups the same case
///     list client-side.
/// </summary>
/// <remarks>
///     Every case is enriched across contexts via ACL facades (alert
///     severity/problem, plot name/location, producer name) and carries the
///     accepted/latest proposal cost. <c>FieldStage</c> is derived only for
///     ACCEPTED cases from the downstream treatment-prescription /
///     execution / outcome lifecycle.
/// </remarks>
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
    /// <summary>A single specialist case with its request status and (for accepted) field stage.</summary>
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
