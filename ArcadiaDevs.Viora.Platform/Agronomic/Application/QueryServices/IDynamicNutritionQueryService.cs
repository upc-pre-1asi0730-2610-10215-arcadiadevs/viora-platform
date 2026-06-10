using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;

/// <summary>
///     Application contract for dynamic nutrition plan queries.
/// </summary>
public interface IDynamicNutritionQueryService
{
    /// <summary>
    ///     Returns the active nutrition plan for a specific plot.
    /// </summary>
    /// <param name="query">The query containing the plot identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    ///     A <see cref="Result{TValue,TError}"/> containing a <see cref="DynamicNutritionPlanDto"/> on success,
    ///     or an <see cref="Error"/> describing the failure.
    /// </returns>
    Task<Result<DynamicNutritionPlanDto, Error>> Handle(
        GetDynamicNutritionPlanQuery query,
        CancellationToken cancellationToken = default);
}