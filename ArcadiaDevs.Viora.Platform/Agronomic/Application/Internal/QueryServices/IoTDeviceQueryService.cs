using ArcadiaDevs.Viora.Platform.Agronomic.Application.ReadModels;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

/// <summary>
///     Application contract for IoT device queries.
///     <para>
///         Both overloads return <see cref="IoTDeviceReadout"/> (D-D11) so the
///         GET endpoint can expose telemetry alongside the aggregate fields.
///     </para>
/// </summary>
public interface IIoTDeviceQueryService
{
    /// <summary>
    ///     Returns all IoT devices for a given plot, paired with their current
    ///     (simulated) telemetry and derived health, provided the requesting
    ///     user owns the plot.
    /// </summary>
    /// <param name="query">The query containing the plot and authenticated user identifiers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    ///     A <see cref="Result{TValue, TError}"/> containing the device readouts
    ///     on success, or a <see cref="Error"/> describing the failure (not
    ///     found, inactive, or ownership violation).
    /// </returns>
    Task<Result<IEnumerable<IoTDeviceReadout>, Error>> Handle(
        GetIoTDevicesByPlotIdQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns all IoT devices across every plot owned by a user, paired
    ///     with their current (simulated) telemetry and derived health.
    ///     <para>
    ///         Backs the dashboard aggregate Water Stress view. Implementation
    ///         is added in T1.17.0-7; the interface declaration is in T1.17.0-6
    ///         so the build is green and the concrete class can be wired in
    ///         DI before the body is filled in.
    ///     </para>
    /// </summary>
    /// <param name="query">The query containing the owning user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    ///     A <see cref="Result{TValue, TError}"/> containing the device readouts
    ///     (possibly empty) on success.
    /// </returns>
    Task<Result<IEnumerable<IoTDeviceReadout>, Error>> Handle(
        GetIoTDevicesByUserIdQuery query,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     Concrete query service that produces <see cref="IoTDeviceReadout"/>
///     readmodels for the IoT device GET endpoints. Singleton-free; scoped
///     lifetime to match the sibling <c>MonitoringSummaryQueryService</c>.
///     <para>
///         Devices are enriched with their current telemetry by the
///     </para>
/// </summary>
/// <remarks>
///     This is the C# port of the OS <c>IoTDeviceQueryService.java</c> (150 lines).
///     The 5 OS dependencies (IoTDeviceRepository, PlotRepository,
///     AgronomicStatisticRepository, SoilReadingSimulator, SensorHealthEvaluator)
///     are injected, plus <see cref="IUnitOfWork"/> for read-only discipline
///     (D-D6) and <see cref="IClock"/> to pass <c>now</c> to the simulator
///     (D-D4). The unit-of-work is NOT flushed (this is a read-side service).
///     <para>
///     The interface signature change (<c>IEnumerable&lt;IoTDevice&gt;</c> →
///     <c>IEnumerable&lt;IoTDeviceReadout&gt;</c>) is the central D-D11 fix:
///     the existing WA controller was calling a method with no DI binding, so
///     the endpoint threw at resolve time. Adding the concrete class
///     alongside the interface change (same commit) keeps the build green and
///     activates the GET endpoint for the first time.
/// </para>
/// </remarks>
public sealed class IoTDeviceQueryService(
    IIoTDeviceRepository ioTDeviceRepository,
    IPlotRepository plotRepository,
    IAgronomicStatisticRepository agronomicStatisticRepository,
    ISoilReadingSimulator soilReadingSimulator,
    ISensorHealthEvaluator sensorHealthEvaluator,
    IUnitOfWork unitOfWork,
    IClock clock) : IIoTDeviceQueryService
{
    /// <inheritdoc />
    public async Task<Result<IEnumerable<IoTDeviceReadout>, Error>> Handle(
        GetIoTDevicesByPlotIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);

        if (plot is null || !plot.IsActive)
        {
            return new Result<IEnumerable<IoTDeviceReadout>, Error>.Failure(
                new Error("PLOT_NOT_FOUND", $"Plot {query.PlotId} not found or inactive."));
        }

        if (plot.OwnerUserId != query.AuthenticatedUserId)
        {
            return new Result<IEnumerable<IoTDeviceReadout>, Error>.Failure(
                new Error("FORBIDDEN", $"User {query.AuthenticatedUserId} does not own plot {query.PlotId}."));
        }

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);
        var latestNdvi = await LatestNdviForPlotAsync(plot.Id, cancellationToken);

        var devices = await ioTDeviceRepository.FindAllByPlotIdAsync(plot.Id);
        var readouts = devices.Select(d => ToReadout(d, plot, latestNdvi, now)).ToList();

        return new Result<IEnumerable<IoTDeviceReadout>, Error>.Success(readouts);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     T1.17.0-6 placeholder — the real implementation lands in T1.17.0-7.
    ///     Throwing now (instead of silently returning an empty list) makes any
    ///     accidental caller of the by-user endpoint fail loudly with a clear
    ///     "1.17.0-7" message, so the apply-progress audit trail is intact.
    /// </remarks>
    public Task<Result<IEnumerable<IoTDeviceReadout>, Error>> Handle(
        GetIoTDevicesByUserIdQuery query,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("IoTDeviceQueryService.Handle(GetIoTDevicesByUserIdQuery) is implemented in T1.17.0-7.");
    }

    /// <summary>
    ///     Builds a device readout, simulating its current telemetry from the plot.
    ///     Mirrors <c>IoTDeviceQueryService.java:131-142</c>. The
    ///     <c>device.ActivationCode?.DeviceType() ?? IoTDeviceType.WeatherStation</c>
    ///     fallback matches the OS line 134-136 (null type → weather station,
    ///     which reports all 3 metrics).
    /// </summary>
    private IoTDeviceReadout ToReadout(
        IoTDevice device,
        Plot plot,
        double? latestNdvi,
        DateTimeOffset now)
    {
        var location = plot.PolygonCoordinates.Centroid();
        var type = device.ActivationCode?.DeviceType() ?? IoTDeviceType.WeatherStation;

        var readings = soilReadingSimulator.Simulate(
            device.ActivationCode, type, location, latestNdvi, now);

        var health = sensorHealthEvaluator.Evaluate(readings);

        return new IoTDeviceReadout(device, readings, health);
    }

    /// <summary>
    ///     Most recent NDVI for a plot, or <c>null</c> when the plot has no
    ///     statistics yet. Mirrors <c>IoTDeviceQueryService.java:144-149</c>.
    /// </summary>
    private async Task<double?> LatestNdviForPlotAsync(int plotId, CancellationToken ct)
    {
        var stat = await agronomicStatisticRepository.FindLatestByPlotIdAsync(plotId, ct);
        return stat is null ? null : stat.NdviValue;
    }
}
