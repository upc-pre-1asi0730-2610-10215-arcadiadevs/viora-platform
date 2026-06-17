using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;

/// <summary>
/// Service that handles commands related to symptoms catalog.
/// </summary>
public interface ISymptomCommandService
{
    /// <summary>
    /// Handles the seeding of predefined symptoms into the database.
    /// </summary>
    /// <param name="command">The command to seed symptoms.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Handle(SeedSymptomsCommand command, CancellationToken cancellationToken = default);
}
