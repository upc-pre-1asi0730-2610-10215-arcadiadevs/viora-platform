using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     The user-session repository.
/// </summary>
public class UserSessionRepository(AppDbContext context)
    : BaseRepository<UserSession>(context), IUserSessionRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSession>> FindByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await Context.Set<UserSession>()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
