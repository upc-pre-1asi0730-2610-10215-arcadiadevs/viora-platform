using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Transform;

/// <summary>
///     Assembles a <see cref="CreateOrUpdateProfileCommand" /> from a <see cref="CreateOrUpdateProfileResource" />.
/// </summary>
public static class CreateOrUpdateProfileCommandFromResourceAssembler
{
    /// <summary>
    ///     Converts a <see cref="CreateOrUpdateProfileResource" /> to a <see cref="CreateOrUpdateProfileCommand" />.
    /// </summary>
    /// <param name="resource">The profile resource.</param>
    /// <param name="userId">The user id from the route.</param>
    /// <returns>The profile command.</returns>
    public static CreateOrUpdateProfileCommand ToCommand(this CreateOrUpdateProfileResource resource, int userId)
    {
        return new CreateOrUpdateProfileCommand(
            userId,
            resource.FullName,
            resource.Email,
            resource.Phone,
            resource.JobTitle,
            resource.Language,
            resource.Location,
            resource.SpecialtyArea,
            resource.PhotoUrl,
            resource.Latitude,
            resource.Longitude,
            resource.ServiceRadiusKm,
            resource.ServiceTags,
            resource.Availability,
            resource.ShowProBadge);
    }
}
