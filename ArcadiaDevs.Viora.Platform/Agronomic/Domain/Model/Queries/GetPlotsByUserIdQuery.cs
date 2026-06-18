namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

public record GetPlotsByUserIdQuery(int UserId, bool IncludeCurrentImagery = false);
