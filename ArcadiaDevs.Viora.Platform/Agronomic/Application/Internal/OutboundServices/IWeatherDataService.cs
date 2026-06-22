using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.OutboundServices;

/// <summary>
///     Outbound service interface for retrieving weather data.
/// </summary>
public interface IWeatherDataService
{
    /// <summary>
    ///     Retrieves the current weather snapshot for the location of a plot.
    /// </summary>
    Task<WeatherSnapshot?> GetCurrentWeatherSnapshotAsync(Plot plot, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves historical weather observations for the location of a plot.
    /// </summary>
    Task<WeatherHistory?> GetWeatherHistoryAsync(Plot plot, DateRange range, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Describes the weather source and its freshness for a plot.
    /// </summary>
    Task<DataSourceMetadata> DescribeSourceAsync(Plot plot, CancellationToken cancellationToken = default);
}
