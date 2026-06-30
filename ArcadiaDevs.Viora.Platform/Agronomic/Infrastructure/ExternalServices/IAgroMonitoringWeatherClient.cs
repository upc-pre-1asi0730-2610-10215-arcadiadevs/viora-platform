using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

/// <summary>
///     Outbound port for the AgroMonitoring external API, scoped to the
///     operations required by the weather integration.
/// </summary>
/// <remarks>
///     <para>
///         Extracting this port keeps the new
///         <see cref="AgroMonitoringWeatherDataService"/> testable with
///         standard mocking tools (NSubstitute, Moq, etc.). The concrete
///         <see cref="AgroMonitoringApiClient"/> implements it alongside
///         the rest of its public surface (polygon registration, NDVI,
///         imagery, tiles).
///     </para>
///     <para>
///         This mirrors the existing pattern in the codebase, where
///         <c>IAgroMonitoringImageryService</c> is implemented by
///         <c>AgroMonitoringImageryServiceAdapter</c>.
///     </para>
/// </remarks>
public interface IAgroMonitoringWeatherClient
{
    /// <summary>
    ///     Fetches accumulated temperature data for a location. The
    ///     <c>threshold</c> is a baseline temperature in Kelvin; the
    ///     returned series contains the temperature value (in Kelvin)
    ///     for each reading.
    /// </summary>
    Task<Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>> GetAccumulatedTemperatureAsync(
        decimal latitude,
        decimal longitude,
        System.DateTimeOffset start,
        System.DateTimeOffset end,
        double threshold = 273.15,
        CancellationToken cancellationToken = default);
}
