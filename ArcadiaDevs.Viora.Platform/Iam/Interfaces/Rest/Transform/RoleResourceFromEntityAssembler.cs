using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles a RoleResource from a Role entity.
 * </summary>
 */
public static class RoleResourceFromEntityAssembler
{
    public static RoleResource ToResource(this Role role)
    {
        return new RoleResource(role.Id, role.Name, role.Description);
    }
}
