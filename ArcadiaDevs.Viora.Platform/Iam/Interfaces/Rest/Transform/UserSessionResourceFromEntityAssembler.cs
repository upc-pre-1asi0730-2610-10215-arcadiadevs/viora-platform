using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;

/**
 * <summary>
 *     Assembles a UserSessionResource from a UserSession entity.
 * </summary>
 */
public static class UserSessionResourceFromEntityAssembler
{
    public static UserSessionResource ToResource(this UserSession session)
    {
        return new UserSessionResource(session.Id, session.UserAgent, session.LastActiveAt, session.IsCurrent);
    }
}
