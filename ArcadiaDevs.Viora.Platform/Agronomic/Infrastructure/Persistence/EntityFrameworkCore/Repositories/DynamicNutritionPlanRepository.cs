using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class DynamicNutritionPlanRepository(AppDbContext context) 
    : BaseRepository<DynamicNutritionPlan>(context), IDynamicNutritionPlanRepository
{
    public async Task<DynamicNutritionPlan?> FindActiveByPlotIdAsync(int plotId, CancellationToken cancellationToken = default)
    {
        return await Context.Set<DynamicNutritionPlan>()
            .Where(p => p.PlotId == plotId && p.Status == ENutritionPlanStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
