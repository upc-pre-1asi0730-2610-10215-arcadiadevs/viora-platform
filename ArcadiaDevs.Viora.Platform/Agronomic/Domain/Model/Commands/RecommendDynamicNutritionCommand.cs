namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

public record RecommendDynamicNutritionCommand(int UserId, int PlotId, long? AlertId = null);
