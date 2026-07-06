using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles a VerifyCommand from a VerifyResource.
 * </summary>
 */
public static class VerifyCommandFromResourceAssembler
{
    public static VerifyCommand ToCommand(this VerifyResource resource)
    {
        return new VerifyCommand(resource.Token);
    }
}
