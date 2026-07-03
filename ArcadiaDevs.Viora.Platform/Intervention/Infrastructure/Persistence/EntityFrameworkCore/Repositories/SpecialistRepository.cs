using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="ISpecialistRepository" />.
/// </summary>
public class SpecialistRepository(AppDbContext context)
    : BaseRepository<Specialist>(context), ISpecialistRepository
{
    public async Task<Specialist?> FindByProfileUserIdAsync(int profileUserId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Specialist>()
            .FirstOrDefaultAsync(s => s.ProfileUserId == profileUserId, cancellationToken);
    }

    public async Task<bool> ExistsByProfileUserIdAsync(int profileUserId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Specialist>()
            .AnyAsync(s => s.ProfileUserId == profileUserId, cancellationToken);
    }
}
