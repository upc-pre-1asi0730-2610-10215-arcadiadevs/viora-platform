using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles a SignInCommand from a SignInResource.
 * </summary>
 */
public static class SignInCommandFromResourceAssembler
{
    public static SignInCommand ToCommand(this SignInResource resource)
    {
        return new SignInCommand(resource.Username, resource.Password);
    }
}
