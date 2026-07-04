using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;

/// <summary>
///     Service that handles commands related to the Specialist catalog.
/// </summary>
public interface ISpecialistCommandService
{
    /// <summary>
    ///     Handles the seeding of demo specialists (and their backing
    ///     Role=Specialist Profile rows) into the database.
    /// </summary>
    /// <param name="command">The command to seed specialists.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Handle(SeedSpecialistsCommand command, CancellationToken cancellationToken = default);
}
