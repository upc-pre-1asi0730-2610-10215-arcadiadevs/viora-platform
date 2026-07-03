using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="IInterventionRequestRepository" />.
/// </summary>
public class InterventionRequestRepository(AppDbContext context)
    : BaseRepository<InterventionRequest>(context), IInterventionRequestRepository
{
    public async Task<IReadOnlyList<InterventionRequest>> ListByGrowerIdAsync(
        int growerId,
        long? plotId,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<InterventionRequest>()
            .Where(r => r.GrowerId == growerId);

        if (plotId is not null)
        {
            query = query.Where(r => r.PlotId == plotId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }
}
