namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;

public interface IPlotDetailMetadataProvider
{
    Task<PlotMetadata?> FindByPlotIdAsync(int plotId, CancellationToken ct = default);
}

public sealed record PlotMetadata(
    DateTimeOffset RegisteredAt,
    DateTimeOffset? LastConfigurationUpdateAt,
    MonitoringIntegrationMetadata? MonitoringIntegration,
    List<DeviceMetadata> Devices);

public sealed record MonitoringIntegrationMetadata(
    DateTimeOffset LinkedAt,
    DateTimeOffset? LastCheckedAt,
    DateTimeOffset? ImageryCaptureAt);

public sealed record DeviceMetadata(
    long DeviceId,
    DateTimeOffset LinkedAt,
    DateTimeOffset? LastActivityAt);
