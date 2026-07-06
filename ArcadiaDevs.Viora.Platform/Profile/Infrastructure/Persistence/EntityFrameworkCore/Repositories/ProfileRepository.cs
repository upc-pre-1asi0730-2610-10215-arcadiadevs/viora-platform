using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Profile.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Repository for the Profile aggregate.
/// </summary>
public class ProfileRepository(AppDbContext context)
    : BaseRepository<ProfileAggregate>(context), IProfileRepository
{
    /// <inheritdoc />
    public async Task<ProfileAggregate?> FindByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await Context.Set<ProfileAggregate>()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProfileAggregate>> FindByRoleAsync(ProfileRole role, CancellationToken ct = default)
    {
        return await Context.Set<ProfileAggregate>()
            .Where(p => p.Role == role)
            .ToListAsync(ct);
    }
}
