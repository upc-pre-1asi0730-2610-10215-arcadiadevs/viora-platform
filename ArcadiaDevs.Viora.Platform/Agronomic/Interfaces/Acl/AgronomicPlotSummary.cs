namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;

/// <summary>
///     Summary of an agronomic plot exposed through the anti-corruption layer.
/// </summary>
/// <param name="Name">The name of the plot.</param>
/// <param name="AgroMonitoringCenter">The center coordinates reported by AgroMonitoring.</param>
/// <param name="AreaSize">The area size of the plot.</param>
public record AgronomicPlotSummary(string Name, string AgroMonitoringCenter, double AreaSize);
