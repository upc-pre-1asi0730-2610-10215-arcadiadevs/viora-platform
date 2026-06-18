using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class AgroMonitoringPlotIntegrationRepository(AppDbContext context)
    : BaseRepository<AgroMonitoringPlotIntegration>(context), IAgroMonitoringPlotIntegrationRepository
{
    public async Task<AgroMonitoringPlotIntegration?> FindByPlotIdAsync(int plotId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<AgroMonitoringPlotIntegration>()
            .FirstOrDefaultAsync(i => i.PlotId == plotId, cancellationToken);
    }
}
