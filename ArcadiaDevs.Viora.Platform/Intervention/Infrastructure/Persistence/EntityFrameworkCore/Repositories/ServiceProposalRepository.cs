using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="IServiceProposalRepository" />.
/// </summary>
public class ServiceProposalRepository(AppDbContext context)
    : BaseRepository<ServiceProposal>(context), IServiceProposalRepository
{
    public async Task<IReadOnlyList<ServiceProposal>> ListByInterventionRequestIdAsync(
        int interventionRequestId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ServiceProposal>()
            .Where(sp => sp.InterventionRequestId == interventionRequestId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceProposal>> FindBySpecialistIdAsync(
        int specialistId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<ServiceProposal>()
            .Where(sp => sp.SpecialistId == specialistId)
            .ToListAsync(cancellationToken);
    }
}
