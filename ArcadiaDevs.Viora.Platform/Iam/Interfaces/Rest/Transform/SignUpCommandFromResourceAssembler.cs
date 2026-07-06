using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles a SignUpCommand from a SignUpResource.
 * </summary>
 */
public static class SignUpCommandFromResourceAssembler
{
    public static SignUpCommand ToCommand(this SignUpResource resource)
    {
        return new SignUpCommand(
            resource.Username,
            resource.Password,
            resource.Email,
            resource.FullName,
            resource.Role,
            resource.ReferralCode);
    }
}