using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="InterventionRequest" />.
/// </summary>
public interface IInterventionRequestCommandService
{
    /// <summary>
    ///     Creates a new intervention request (REQ-IREQ-1), validating
    ///     <c>growerId</c>/<c>plotId</c>/<c>specialistId</c>/<c>alertId</c>
    ///     through their respective ACL facades.
    /// </summary>
    Task<Result<InterventionRequest, Error>> Handle(
        CreateInterventionRequestCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Declines an existing intervention request (REQ-IREQ-3, no
    ///     self-guard against the current status).
    /// </summary>
    Task<Result<InterventionRequest, Error>> Handle(
        DeclineInterventionRequestCommand command,
        CancellationToken cancellationToken = default);
}
