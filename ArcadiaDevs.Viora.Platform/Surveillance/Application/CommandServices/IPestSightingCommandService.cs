using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;

/// <summary>
/// Service that handles commands related to pest sighting reports.
/// </summary>
public interface IPestSightingCommandService
{
    /// <summary>
    /// Handles the creation of a new pest sighting report.
    /// </summary>
    /// <param name="command">The command containing the report data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the created report or an error.</returns>
    Task<Result<PestSightingReport, Error>> Handle(CreatePestSightingReportCommand command, CancellationToken cancellationToken = default);
}
