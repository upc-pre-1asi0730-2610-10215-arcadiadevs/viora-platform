using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles a UserResource from a User entity.
 * </summary>
 */
public static class UserResourceFromEntityAssembler
{
    public static UserResource ToResource(this User user)
    {
        return new UserResource(user.Id, user.Username, user.Active, user.Verified);
    }
}
