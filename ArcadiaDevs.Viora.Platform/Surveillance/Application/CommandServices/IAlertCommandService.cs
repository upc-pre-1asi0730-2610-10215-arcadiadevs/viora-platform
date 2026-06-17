using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;

/// <summary>
/// Service that handles commands related to automated alerts.
/// </summary>
public interface IAlertCommandService
{
    /// <summary>
    /// Handles the creation of a new automated alert.
    /// </summary>
    /// <param name="command">The command containing the alert data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the created alert or an error.</returns>
    Task<Result<Alert, Error>> Handle(CreateAlertCommand command, CancellationToken cancellationToken = default);
}
