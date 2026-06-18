using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;

/// <summary>
///     Represents the persistent cache for AgroMonitoring integration.
/// </summary>
public class AgroMonitoringPlotIntegration
{
    public int Id { get; private set; }
    public int PlotId { get; private set; }
    public string ExternalPolygonId { get; set; } = null!;
    public string BoundaryFingerprint { get; set; } = null!;
    public string? ProviderImageryId { get; set; }
    public string? TileUrl { get; set; }
    public DateTimeOffset? CaptureDate { get; set; }
    public double? NdviMean { get; set; }
    public double? CloudPercentage { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }

    /// <summary>
    ///     Required by Entity Framework.
    /// </summary>
    protected AgroMonitoringPlotIntegration() { }

    public AgroMonitoringPlotIntegration(int plotId, string externalPolygonId, string boundaryFingerprint)
    {
        PlotId = plotId;
        ExternalPolygonId = externalPolygonId;
        BoundaryFingerprint = boundaryFingerprint;
    }
}
