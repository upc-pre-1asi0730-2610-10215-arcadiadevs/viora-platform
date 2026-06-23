using System.Security.Cryptography;
using System.Text;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

public class AgroMonitoringImageryServiceAdapter(
    AgroMonitoringApiClient apiClient,
    IAgroMonitoringPlotIntegrationRepository integrationRepository,
    IUnitOfWork unitOfWork) : IAgroMonitoringImageryService
{
    private const int CacheTtlDays = 1;

    public async Task<bool> IsPlotLinkedAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        var integration = await integrationRepository.FindByPlotIdAsync(plot.Id, cancellationToken);
        if (integration == null) return false;

        var fingerprint = BoundaryFingerprint(plot);
        return integration.BoundaryFingerprint == fingerprint;
    }

    public async Task FindCurrentImageryAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        var integration = await integrationRepository.FindByPlotIdAsync(plot.Id, cancellationToken);
        var fingerprint = BoundaryFingerprint(plot);

        // Si no existe o cambiaron los límites, re-registrar el polígono
        if (integration == null || integration.BoundaryFingerprint != fingerprint)
        {
            var createResult = await apiClient.CreatePolygonAsync(plot.PlotName, plot.PolygonCoordinates.Points, cancellationToken);
            if (!createResult.IsSuccess) return;

            var newExternalId = ((Result<AgroMonitoringPolygonResponse, Error>.Success)createResult).Value.Id;
            if (integration == null)
            {
                integration = new AgroMonitoringPlotIntegration(plot.Id, newExternalId, fingerprint);
                await integrationRepository.AddAsync(integration, cancellationToken);
            }
            else
            {
                integration.ExternalPolygonId = newExternalId;
                integration.BoundaryFingerprint = fingerprint;
                integrationRepository.Update(integration);
            }
        }

        // Si se actualizó hace poco, usar la caché
        if (WasCheckedRecently(integration))
        {
            return;
        }

        // Buscar imagen satelital (últimos 30 días)
        var end = DateTimeOffset.UtcNow;
        var start = end.AddDays(-30);
        var imagesResult = await apiClient.GetImagesAsync(integration.ExternalPolygonId, start, end, cancellationToken);

        if (!imagesResult.IsSuccess || !((Result<IReadOnlyList<AgroMonitoringImageResponse>, Error>.Success)imagesResult).Value.Any())
        {
            integration.LastCheckedAt = DateTimeOffset.UtcNow;
            integrationRepository.Update(integration);
            await unitOfWork.CompleteAsync(cancellationToken);
            return;
        }

        var images = ((Result<IReadOnlyList<AgroMonitoringImageResponse>, Error>.Success)imagesResult).Value;
        var latestImage = images
            .OrderByDescending(img => img.Dt)
            .FirstOrDefault();

        if (latestImage != null)
        {
            integration.ProviderImageryId = latestImage.Dt.ToString();
            integration.TileUrl = RemoveApiKey(latestImage.Tile.Ndvi);
            integration.CaptureDate = DateTimeOffset.FromUnixTimeSeconds(latestImage.Dt);
            integration.CloudPercentage = latestImage.Cl;
        }

        integration.LastCheckedAt = DateTimeOffset.UtcNow;
        integrationRepository.Update(integration);
        await unitOfWork.CompleteAsync(cancellationToken);
    }

    public async Task<byte[]?> FetchCurrentNdviTileAsync(Plot plot, int zoom, int x, int y, CancellationToken cancellationToken = default)
    {
        var integration = await integrationRepository.FindByPlotIdAsync(plot.Id, cancellationToken);
        if (integration?.TileUrl == null)
        {
            return null;
        }

        // Reemplazar coordenadas
        var url = integration.TileUrl
            .Replace("{z}", zoom.ToString())
            .Replace("{x}", x.ToString())
            .Replace("{y}", y.ToString());

        var bytesResult = await apiClient.GetTileAsync(url, cancellationToken);
        return bytesResult.IsSuccess ? ((Result<byte[], Error>.Success)bytesResult).Value : null;
    }

    private static string BoundaryFingerprint(Plot plot)
    {
        var sb = new StringBuilder();
        foreach (var point in plot.PolygonCoordinates.Points)
        {
            sb.Append($"{point.Longitude},{point.Latitude};");
        }

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool WasCheckedRecently(AgroMonitoringPlotIntegration integration)
    {
        if (!integration.LastCheckedAt.HasValue) return false;
        var diff = DateTimeOffset.UtcNow - integration.LastCheckedAt.Value;
        return diff.TotalDays < CacheTtlDays;
    }

    private static string RemoveApiKey(string url)
    {
        var uri = new UriBuilder(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        query.Remove("appid");
        uri.Query = query.ToString();
        return uri.Uri.ToString();
    }
}
