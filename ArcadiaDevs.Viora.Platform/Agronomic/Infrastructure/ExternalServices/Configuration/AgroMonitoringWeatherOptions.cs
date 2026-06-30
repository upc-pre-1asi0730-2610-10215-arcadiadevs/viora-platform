namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices.Configuration;

/// <summary>
///     Strongly-typed options for the AgroMonitoring weather integration.
/// </summary>
/// <remarks>
///     Bound from configuration section <c>Agronomic:Weather:AgroMonitoring</c>.
///     Validated at startup by <see cref="AgroMonitoringWeatherOptionsValidator"/>
///     (CC-5 fail-fast in all environments).
/// </remarks>
public class AgroMonitoringWeatherOptions
{
    /// <summary>
    ///     The configuration section path used for binding.
    /// </summary>
    public const string SectionName = "Agronomic:Weather:AgroMonitoring";

    /// <summary>
    ///     The AgroMonitoring API key. Must be non-empty.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
