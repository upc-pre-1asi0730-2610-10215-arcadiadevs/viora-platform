using ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="SeedSpecialistsCommand" />. Now a documented no-op
///     — mirrors OS's own <c>SpecialistSeedCommandService.handle</c>, which
///     was disabled once real specialist matching (live-derived from
///     <c>Profile</c>, Role=Specialist) replaced the fixed demo catalog this
///     seed used to populate. Kept only so existing DI wiring/callers don't
///     break; a specialist's presence in the system now comes entirely from
///     real sign-ups (<c>UserCommandService.Handle(SignUpCommand)</c> with
///     <c>role=Specialist</c>), not from this seed.
/// </summary>
public class SpecialistCommandService(ILogger<SpecialistCommandService> logger) : ISpecialistCommandService
{
    public Task Handle(SeedSpecialistsCommand command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Skipping demo Specialist seed — specialists are now sourced live from real Profile (Role=Specialist) rows.");
        return Task.CompletedTask;
    }
}
