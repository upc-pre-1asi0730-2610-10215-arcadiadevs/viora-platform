using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles a ResendVerificationCommand from a ResendVerificationResource.
 * </summary>
 */
public static class ResendVerificationCommandFromResourceAssembler
{
    public static ResendVerificationCommand ToCommand(this ResendVerificationResource resource)
    {
        return new ResendVerificationCommand(resource.Username);
    }
}
