using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles an AssignRoleCommand from user id and resource
 * </summary>
 */
public static class AssignRoleCommandFromResourceAssembler
{
    public static AssignRoleCommand ToCommandFromResource(int userId, AssignRoleResource resource)
    {
        return new AssignRoleCommand(userId, resource.RoleName);
    }
}
