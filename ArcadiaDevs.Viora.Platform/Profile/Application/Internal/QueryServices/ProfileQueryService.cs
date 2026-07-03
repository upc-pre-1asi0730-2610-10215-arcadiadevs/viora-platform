using ArcadiaDevs.Viora.Platform.Profile.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Profile.Application.Internal.QueryServices;

/// <summary>
///     Handles profile read queries by delegating to the repository.
/// </summary>
public class ProfileQueryService(IProfileRepository profileRepository) : IProfileQueryService
{
    /// <inheritdoc />
    public async Task<ProfileAggregate?> Handle(GetProfileByUserIdQuery query, CancellationToken ct)
    {
        return await profileRepository.FindByUserIdAsync(query.UserId, ct);
    }
}
