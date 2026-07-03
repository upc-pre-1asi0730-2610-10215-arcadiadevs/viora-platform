using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Queries;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Profile.Application.QueryServices;

/// <summary>
///     Query service for profile reads.
/// </summary>
public interface IProfileQueryService
{
    /// <summary>
    ///     Handles a get-profile-by-user-id query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The profile if found, otherwise null.</returns>
    Task<ProfileAggregate?> Handle(GetProfileByUserIdQuery query, CancellationToken ct);
}
