using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

public interface IDynamicNutritionPlanRepository : IBaseRepository<DynamicNutritionPlan>
{
    Task<DynamicNutritionPlan?> FindActiveByPlotIdAsync(int plotId, CancellationToken cancellationToken = default);
}
