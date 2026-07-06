using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Resources;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Transform;

/// <summary>
///     Assembles a <see cref="ProfileResource" /> from a <see cref="ProfileAggregate" /> entity.
/// </summary>
public static class ProfileResourceFromEntityAssembler
{
    /// <summary>
    ///     Converts a <see cref="ProfileAggregate" /> entity to a <see cref="ProfileResource" />.
    /// </summary>
    /// <param name="profile">The profile entity.</param>
    /// <returns>The profile resource.</returns>
    public static ProfileResource ToResource(this ProfileAggregate profile)
    {
        return new ProfileResource(
            profile.UserId,
            profile.Role.ToString(),
            profile.FullName,
            profile.Email,
            profile.Phone,
            profile.JobTitle,
            profile.Language,
            profile.Location,
            profile.SpecialtyArea,
            profile.PhotoUrl);
    }
}
