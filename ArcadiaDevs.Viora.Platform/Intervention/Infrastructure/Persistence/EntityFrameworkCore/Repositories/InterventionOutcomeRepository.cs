using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="IInterventionOutcomeRepository" />.
/// </summary>
public class InterventionOutcomeRepository(AppDbContext context)
    : BaseRepository<InterventionOutcome>(context), IInterventionOutcomeRepository
{
    public async Task<InterventionOutcome?> FindByInterventionExecutionIdAsync(
        int interventionExecutionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<InterventionOutcome>()
            .FirstOrDefaultAsync(io => io.InterventionExecutionId == interventionExecutionId, cancellationToken);
    }
}
