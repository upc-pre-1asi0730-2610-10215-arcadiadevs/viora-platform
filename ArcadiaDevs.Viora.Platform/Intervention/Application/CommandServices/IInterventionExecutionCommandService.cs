using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="InterventionExecution" />.
/// </summary>
public interface IInterventionExecutionCommandService
{
    /// <summary>
    ///     Certifies a new execution (REQ-IE-1), validating
    ///     <c>TreatmentPrescriptionId</c> exists and is <c>PRESCRIBED</c>
    ///     (409 otherwise), and that no execution already exists for it
    ///     (REQ-IE-2 idempotency).
    /// </summary>
    Task<Result<InterventionExecution, Error>> Handle(
        CertifyInterventionExecutionCommand command,
        CancellationToken cancellationToken = default);
}
