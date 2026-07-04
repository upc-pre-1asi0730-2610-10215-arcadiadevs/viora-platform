namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to create an <see cref="Aggregates.InterventionRequest" />
///     (REQ-IREQ-1). <c>GrowerId</c>, <c>PlotId</c>, <c>SpecialistId</c>,
///     and <c>AlertId</c> (when provided) are validated against their
///     respective ACLs by the command service before the aggregate is
///     constructed.
/// </summary>
public record CreateInterventionRequestCommand(
    int GrowerId,
    long PlotId,
    int SpecialistId,
    long? AlertId,
    string Reason,
    string Message);
