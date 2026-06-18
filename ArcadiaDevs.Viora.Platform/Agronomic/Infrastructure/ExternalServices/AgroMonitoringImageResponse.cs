using System.Text.Json.Serialization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

public record AgroMonitoringImageResponse(
    long Dt,
    double Cl,
    AgroMonitoringProductUrls Tile,
    AgroMonitoringProductUrls Stats
);

public record AgroMonitoringProductUrls(
    string Ndvi
);
