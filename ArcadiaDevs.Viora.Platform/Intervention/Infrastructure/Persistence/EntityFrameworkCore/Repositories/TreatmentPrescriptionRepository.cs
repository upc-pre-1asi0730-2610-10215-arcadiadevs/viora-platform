using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="ITreatmentPrescriptionRepository" />.
/// </summary>
public class TreatmentPrescriptionRepository(AppDbContext context)
    : BaseRepository<TreatmentPrescription>(context), ITreatmentPrescriptionRepository
{
    public async Task<TreatmentPrescription?> FindByServiceProposalIdAsync(
        int serviceProposalId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<TreatmentPrescription>()
            .FirstOrDefaultAsync(tp => tp.ServiceProposalId == serviceProposalId, cancellationToken);
    }
}
