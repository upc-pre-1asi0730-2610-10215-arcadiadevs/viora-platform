using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Metadata describing the source and freshness of weather data.
/// </summary>
public record DataSourceMetadata(
    string ProviderName,
    string ConnectivityStatus,
    DateTimeOffset LastSyncAt,
    int SyncIntervalMinutes);
