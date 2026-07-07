using ArcadiaDevs.Viora.Platform.Iam.Domain.Model;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles an AuthenticatedUserResource from an AuthenticatedUser.
 * </summary>
 */
public static class AuthenticatedUserResourceFromEntityAssembler
{
    public static AuthenticatedUserResource ToResource(this AuthenticatedUser authenticatedUser)
    {
        return new AuthenticatedUserResource(authenticatedUser.Id, authenticatedUser.Username, authenticatedUser.Token, authenticatedUser.Role);
    }
}
