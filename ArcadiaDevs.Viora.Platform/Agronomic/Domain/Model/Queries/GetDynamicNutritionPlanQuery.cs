namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

public record GetDynamicNutritionPlanQuery
{
    public int PlotId { get; init; }

    public GetDynamicNutritionPlanQuery(int plotId)
    {
        if (plotId <= 0)
            throw new ArgumentException("GetDynamicNutritionPlanQuery requires a valid PlotId.", nameof(plotId));

        PlotId = plotId;
    }
}