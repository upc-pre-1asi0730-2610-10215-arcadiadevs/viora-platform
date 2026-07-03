using ArcadiaDevs.Viora.Platform.Profile.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Profile.Application.Internal.QueryServices;

/// <summary>
///     Handles profile read queries by delegating to the repository.
/// </summary>
public class ProfileQueryService(IProfileRepository profileRepository) : IProfileQueryService
{
    /// <inheritdoc />
    public async Task<Profile?> Handle(GetProfileByUserIdQuery query, CancellationToken ct)
    {
        return await profileRepository.FindByUserIdAsync(query.UserId, ct);
    }
}
