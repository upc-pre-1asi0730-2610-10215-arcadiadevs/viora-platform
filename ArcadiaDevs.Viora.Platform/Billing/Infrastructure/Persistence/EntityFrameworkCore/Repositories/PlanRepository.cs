using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="IPlanRepository" />.
/// </summary>
public class PlanRepository(AppDbContext context)
    : BaseRepository<Plan>(context), IPlanRepository
{
    public async Task<Plan?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Plan>()
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Plan>()
            .AnyAsync(p => p.Code == code, cancellationToken);
    }
}