using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Acl;

/// <summary>
///     Facade for the agronomic context.
/// </summary>
/// <param name="monitoringSummaryQueryService">
///     The monitoring summary query service.
/// </param>
/// <param name="plotRepository">
///     The plot repository.
/// </param>
public class AgronomicContextFacade(
    IMonitoringSummaryQueryService monitoringSummaryQueryService,
    IPlotRepository plotRepository) : IAgronomicContextFacade
{
    // inheritedDoc
    public async Task<double?> FetchCurrentNdviByReporterAsync(int reporterUserId, CancellationToken cancellationToken = default)
    {
        var query = new GetCurrentMonitoringSummaryQuery(reporterUserId);
        var result = await monitoringSummaryQueryService.Handle(query, cancellationToken);

        if (result is Result<MonitoringSummaryResource, Error>.Success success)
        {
            return (double)success.Value.AverageNdvi;
        }

        return null;
    }

    // inheritedDoc
    public async Task<IReadOnlyDictionary<long, AgronomicPlotSummary>> FetchPlotsByOwnerUserAsync(int ownerUserId, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.FindAllByOwnerUserIdAsync(ownerUserId, cancellationToken);
        var map = new Dictionary<long, AgronomicPlotSummary>();

        foreach (var plot in plots)
        {
            map[plot.Id] = new AgronomicPlotSummary(
                plot.PlotName,
                plot.AgroMonitoringCenter ?? "Unknown",
                (double)plot.AreaSize);
        }

        return map;
    }

    // inheritedDoc
    public async Task<string?> GetPlotNameAsync(long plotId, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync((int)plotId, cancellationToken);
        return plot?.PlotName;
    }

    // inheritedDoc
    public async Task<PlotCardSummary?> GetPlotCardSummaryAsync(long plotId, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync((int)plotId, cancellationToken);

        return plot is null
            ? null
            : new PlotCardSummary(plot.PlotName, plot.Location, plot.CropType, plot.AreaSize);
    }

    // inheritedDoc
    public async Task<int> CountPlotsByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.FindAllByOwnerUserIdAsync(userId, cancellationToken);
        return plots.Count();
    }

    // inheritedDoc
    public async Task<double?> DistanceKmFromPlotCentroidAsync(long plotId, double lat, double lng, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync((int)plotId, cancellationToken);
        var centroid = plot?.PolygonCoordinates?.Centroid();

        return centroid is null
            ? null
            : HaversineKilometers(centroid.Value, (lat, lng));
    }

    // inheritedDoc
    public async Task<IReadOnlyList<NeighborPlot>> FindNeighborPlotsWithinRadiusAsync(
        long referencePlotId,
        double radiusKm,
        CancellationToken cancellationToken = default)
    {
        var reference = await plotRepository.FindByIdAsync((int)referencePlotId, cancellationToken);
        if (reference is null)
        {
            return [];
        }

        var referenceCentroid = reference.PolygonCoordinates?.Centroid();
        if (referenceCentroid is null)
        {
            return [];
        }

        var plots = await plotRepository.ListAsync(cancellationToken);

        return plots
            .Where(plot => plot.Id != referencePlotId)
            .Select(plot => ToNeighbor(plot, referenceCentroid.Value))
            .Where(neighbor => neighbor is not null && neighbor.DistanceKm <= radiusKm)
            .Select(neighbor => neighbor!)
            .ToList();
    }

    /// <summary>
    ///     Builds an anonymized neighbor reference, or null when the plot has no usable geometry.
    /// </summary>
    private static NeighborPlot? ToNeighbor(Plot plot, (double Lat, double Lon) referenceCentroid)
    {
        var centroid = plot.PolygonCoordinates?.Centroid();
        if (centroid is null)
        {
            return null;
        }

        var distanceKm = HaversineKilometers(referenceCentroid, centroid.Value);
        var roundedKm = Math.Round(distanceKm * 10.0) / 10.0;
        return new NeighborPlot(plot.Id, roundedKm);
    }

    /// <summary>
    ///     Great-circle distance (in kilometers) between two points using the haversine formula.
    /// </summary>
    private static double HaversineKilometers((double Lat, double Lon) from, (double Lat, double Lon) to)
    {
        const double earthRadiusKm = 6371.0088;

        var deltaLat = ToRadians(to.Lat - from.Lat);
        var deltaLon = ToRadians(to.Lon - from.Lon);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(ToRadians(from.Lat)) * Math.Cos(ToRadians(to.Lat)) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
