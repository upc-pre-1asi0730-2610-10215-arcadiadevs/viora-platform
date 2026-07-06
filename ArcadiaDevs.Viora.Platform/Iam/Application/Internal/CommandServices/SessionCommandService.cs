using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.Internal.CommandServices;

/**
 * <summary>
 *     Handles user-session commands.
 * </summary>
 */
public class SessionCommandService(
    IUserSessionRepository userSessionRepository,
    IUnitOfWork unitOfWork) : ISessionCommandService
{
    /// <inheritdoc />
    public async Task<Result<Unit, Error>> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        // REQ-CC-3: sessionId is scoped to userId — a session belonging to a
        // different user is treated as not found under this route.
        var session = await userSessionRepository.FindByIdAsync(command.SessionId, cancellationToken);
        if (session == null || session.UserId != command.UserId)
            return new Result<Unit, Error>.Failure(IamErrors.SessionNotFound);

        // REQ-SESS-3: the session tied to the caller's current sign-in cannot
        // be revoked from this endpoint. Also guards REQ-SESS-5 (revoke
        // idempotency) implicitly — once removed, a repeat call falls into the
        // "not found" branch above rather than succeeding silently twice.
        if (session.IsCurrent)
            return new Result<Unit, Error>.Failure(IamErrors.CannotRevokeCurrentSession);

        userSessionRepository.Remove(session);
        await unitOfWork.CompleteAsync(cancellationToken);

        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
