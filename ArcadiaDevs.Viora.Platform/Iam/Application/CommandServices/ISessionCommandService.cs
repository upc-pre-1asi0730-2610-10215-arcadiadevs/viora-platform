using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;

/**
 * <summary>
 *     The session command service interface
 * </summary>
 */
public interface ISessionCommandService
{
    /**
     * <summary>
     *     Handle revoke-session command
     * </summary>
     * <param name="command">The revoke-session command</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>A result indicating success or failure</returns>
     */
    Task<Result<Unit, Error>> Handle(RevokeSessionCommand command, CancellationToken cancellationToken);
}
