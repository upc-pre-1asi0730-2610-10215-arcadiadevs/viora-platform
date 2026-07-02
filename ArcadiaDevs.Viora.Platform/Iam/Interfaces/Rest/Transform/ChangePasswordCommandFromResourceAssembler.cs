using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles a ChangePasswordCommand from a ChangePasswordResource.
 * </summary>
 */
public static class ChangePasswordCommandFromResourceAssembler
{
    public static ChangePasswordCommand ToCommand(this ChangePasswordResource resource, int userId)
    {
        return new ChangePasswordCommand(userId, resource.CurrentPassword, resource.NewPassword);
    }
}
