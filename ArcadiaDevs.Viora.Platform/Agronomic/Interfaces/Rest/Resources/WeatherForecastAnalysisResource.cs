namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record WeatherForecastAnalysisResource
{
    public double? ThermalAnomalyCelsius { get; init; }
    public string OverallRisk { get; init; } = string.Empty;
    public int WarningCount { get; init; }
}
