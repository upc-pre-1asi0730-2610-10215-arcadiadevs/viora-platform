namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for weather forecast analysis from the advisor.
/// </summary>
public record WeatherForecastAnalysisResource
{
    public double? ThermalAnomalyCelsius { get; init; }
    public string OverallRisk { get; init; } = string.Empty;
    public int WarningCount { get; init; }
}
