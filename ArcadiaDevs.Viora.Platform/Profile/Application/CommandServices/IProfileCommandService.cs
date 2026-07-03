using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Profile.Application.CommandServices;

/// <summary>
///     Command service for profile upsert operations.
/// </summary>
public interface IProfileCommandService
{
    /// <summary>
    ///     Handles a create-or-update profile command.
    /// </summary>
    /// <param name="command">The upsert command.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>
    ///     A result containing the profile and a boolean indicating whether it
    ///     was newly created (true) or updated (false).
    /// </returns>
    Task<Result<(Profile Profile, bool Created), Error>> Handle(
        CreateOrUpdateProfileCommand command,
        CancellationToken ct);
}
