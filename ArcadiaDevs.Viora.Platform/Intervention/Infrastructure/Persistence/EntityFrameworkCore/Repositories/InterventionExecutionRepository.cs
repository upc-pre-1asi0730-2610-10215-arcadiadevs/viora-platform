using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Entity Framework Core implementation of the <see cref="IInterventionExecutionRepository" />.
/// </summary>
public class InterventionExecutionRepository(AppDbContext context)
    : BaseRepository<InterventionExecution>(context), IInterventionExecutionRepository
{
    public async Task<InterventionExecution?> FindByTreatmentPrescriptionIdAsync(
        int treatmentPrescriptionId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<InterventionExecution>()
            .FirstOrDefaultAsync(ie => ie.TreatmentPrescriptionId == treatmentPrescriptionId, cancellationToken);
    }
}
