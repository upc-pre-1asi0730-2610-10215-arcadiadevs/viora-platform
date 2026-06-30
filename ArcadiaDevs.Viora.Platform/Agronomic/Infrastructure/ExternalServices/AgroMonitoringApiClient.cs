using System.Net.Http.Json;
using System.Text.Json;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;

/// <summary>
///     Client for the AgroMonitoring external API.
/// </summary>
/// <remarks>
///     Provides methods to register polygons, retrieve NDVI history, and fetch
///     accumulated temperature data. All public methods return
///     <see cref="Result{TValue,TError}"/> to keep error handling explicit.
/// </remarks>
public class AgroMonitoringApiClient : IAgroMonitoringWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<AgroMonitoringApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="AgroMonitoringApiClient"/> class.
    /// </summary>
    public AgroMonitoringApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AgroMonitoringApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _apiKey = configuration["ExternalApis:AgroMonitoring:ApiKey"]
            ?? throw new InvalidOperationException(
                "AgroMonitoring API key is not configured. " +
                "Set ExternalApis:AgroMonitoring:ApiKey in appsettings.json or environment.");
    }

    /// <summary>
    ///     Registers a polygon with AgroMonitoring and returns the assigned polygon ID.
    /// </summary>
    /// <param name="name">Display name for the polygon.</param>
    /// <param name="polygonCoordinates">The polygon vertices.</param>
    /// <returns>A Result containing the polygon response or an error.</returns>
    public async Task<Result<AgroMonitoringPolygonResponse, Error>> CreatePolygonAsync(
        string name,
        IReadOnlyList<GeoPoint> polygonCoordinates,
        CancellationToken cancellationToken = default)
    {
        // AgroMonitoring expects coordinates as [lon, lat] pairs (GeoJSON order).
        var coordinates = polygonCoordinates
            .Select(p => new[] { (double)p.Longitude, (double)p.Latitude })
            .ToArray();

        var body = new
        {
            name,
            geo_json = new
            {
                type = "Feature",
                properties = new { },
                geometry = new
                {
                    type = "Polygon",
                    coordinates = new[] { coordinates }
                }
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/agro/1.0/polygons?appid={_apiKey}",
                body,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "AgroMonitoring polygon creation failed ({StatusCode}): {Body}",
                    response.StatusCode, errorBody);

                return new Result<AgroMonitoringPolygonResponse, Error>.Failure(
                    new Error(
                        "AGROMONITORING_POLYGON_FAILED",
                        $"Polygon creation failed with status {response.StatusCode}"));
            }

            var result = await response.Content.ReadFromJsonAsync<AgroMonitoringPolygonResponse>(
                JsonOptions, cancellationToken);

            if (result is null)
            {
                return new Result<AgroMonitoringPolygonResponse, Error>.Failure(
                    new Error("AGROMONITORING_NULL_RESPONSE", "Polygon response was null"));
            }

            return new Result<AgroMonitoringPolygonResponse, Error>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling AgroMonitoring polygon creation");
            return new Result<AgroMonitoringPolygonResponse, Error>.Failure(
                new Error("AGROMONITORING_NETWORK_ERROR", $"Network error: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Fetches NDVI history for a polygon within a time range.
    /// </summary>
    /// <param name="polygonId">The AgroMonitoring polygon ID.</param>
    /// <param name="start">Start of the time range.</param>
    /// <param name="end">End of the time range.</param>
    /// <returns>A Result containing the NDVI data points or an error.</returns>
    public async Task<Result<IReadOnlyList<AgroMonitoringNdviDataPoint>, Error>> GetNdviHistoryAsync(
        string polygonId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var startUnix = new DateTimeOffset(start.UtcDateTime, TimeSpan.Zero).ToUnixTimeSeconds();
        var endUnix = new DateTimeOffset(end.UtcDateTime, TimeSpan.Zero).ToUnixTimeSeconds();

        try
        {
            var response = await _httpClient.GetAsync(
                $"/agro/1.0/ndvi/history?start={startUnix}&end={endUnix}&polyid={polygonId}&appid={_apiKey}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "AgroMonitoring NDVI history failed ({StatusCode}): {Body}",
                    response.StatusCode, errorBody);

                return new Result<IReadOnlyList<AgroMonitoringNdviDataPoint>, Error>.Failure(
                    new Error(
                        "AGROMONITORING_NDVI_FAILED",
                        $"NDVI history failed with status {response.StatusCode}"));
            }

            var result = await response.Content.ReadFromJsonAsync<List<AgroMonitoringNdviDataPoint>>(
                JsonOptions, cancellationToken);

            return new Result<IReadOnlyList<AgroMonitoringNdviDataPoint>, Error>.Success(
                result?.AsReadOnly() ?? (IReadOnlyList<AgroMonitoringNdviDataPoint>)Array.Empty<AgroMonitoringNdviDataPoint>());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling AgroMonitoring NDVI history");
            return new Result<IReadOnlyList<AgroMonitoringNdviDataPoint>, Error>.Failure(
                new Error("AGROMONITORING_NETWORK_ERROR", $"Network error: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Fetches accumulated temperature data for a location.
    /// </summary>
    /// <param name="latitude">Latitude of the location.</param>
    /// <param name="longitude">Longitude of the location.</param>
    /// <param name="start">Start of the time range.</param>
    /// <param name="end">End of the time range.</param>
    /// <param name="threshold">Temperature threshold in Kelvin.</param>
    /// <returns>A Result containing the temperature data points or an error.</returns>
    public async Task<Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>> GetAccumulatedTemperatureAsync(
        decimal latitude,
        decimal longitude,
        DateTimeOffset start,
        DateTimeOffset end,
        double threshold = 273.15,
        CancellationToken cancellationToken = default)
    {
        var startUnix = new DateTimeOffset(start.UtcDateTime, TimeSpan.Zero).ToUnixTimeSeconds();
        var endUnix = new DateTimeOffset(end.UtcDateTime, TimeSpan.Zero).ToUnixTimeSeconds();

        try
        {
            var response = await _httpClient.GetAsync(
                $"/agro/1.0/weather/history/accumulated_temperature" +
                $"?lat={latitude}&lon={longitude}" +
                $"&start={startUnix}&end={endUnix}" +
                $"&threshold={threshold}" +
                $"&appid={_apiKey}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "AgroMonitoring accumulated temperature failed ({StatusCode}): {Body}",
                    response.StatusCode, errorBody);

                return new Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Failure(
                    new Error(
                        "AGROMONITORING_TEMP_FAILED",
                        $"Temperature history failed with status {response.StatusCode}"));
            }

            var result = await response.Content.ReadFromJsonAsync<List<AgroMonitoringTemperatureDataPoint>>(
                JsonOptions, cancellationToken);

            return new Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Success(
                result?.AsReadOnly() ?? (IReadOnlyList<AgroMonitoringTemperatureDataPoint>)Array.Empty<AgroMonitoringTemperatureDataPoint>());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling AgroMonitoring accumulated temperature");
            return new Result<IReadOnlyList<AgroMonitoringTemperatureDataPoint>, Error>.Failure(
                new Error("AGROMONITORING_NETWORK_ERROR", $"Network error: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Fetches the satellite imagery available for a polygon.
    /// </summary>
    public async Task<Result<IReadOnlyList<AgroMonitoringImageResponse>, Error>> GetImagesAsync(
        string polygonId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var startUnix = new DateTimeOffset(start.UtcDateTime, TimeSpan.Zero).ToUnixTimeSeconds();
        var endUnix = new DateTimeOffset(end.UtcDateTime, TimeSpan.Zero).ToUnixTimeSeconds();

        try
        {
            var response = await _httpClient.GetAsync(
                $"/agro/1.0/image/search?start={startUnix}&end={endUnix}&polyid={polygonId}&appid={_apiKey}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "AgroMonitoring image search failed ({StatusCode}): {Body}",
                    response.StatusCode, errorBody);

                return new Result<IReadOnlyList<AgroMonitoringImageResponse>, Error>.Failure(
                    new Error(
                        "AGROMONITORING_IMAGE_FAILED",
                        $"Image search failed with status {response.StatusCode}"));
            }

            var result = await response.Content.ReadFromJsonAsync<List<AgroMonitoringImageResponse>>(
                JsonOptions, cancellationToken);

            return new Result<IReadOnlyList<AgroMonitoringImageResponse>, Error>.Success(
                result?.AsReadOnly() ?? (IReadOnlyList<AgroMonitoringImageResponse>)Array.Empty<AgroMonitoringImageResponse>());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling AgroMonitoring image search");
            return new Result<IReadOnlyList<AgroMonitoringImageResponse>, Error>.Failure(
                new Error("AGROMONITORING_NETWORK_ERROR", $"Network error: {ex.Message}"));
        }
    }

    /// <summary>
    ///     Downloads a raw tile PNG.
    /// </summary>
    public async Task<Result<byte[], Error>> GetTileAsync(
        string tileUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new UriBuilder(tileUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            query["appid"] = _apiKey;
            uri.Query = query.ToString();

            var response = await _httpClient.GetAsync(uri.Uri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AgroMonitoring tile download failed ({StatusCode})",
                    response.StatusCode);

                return new Result<byte[], Error>.Failure(
                    new Error(
                        "AGROMONITORING_TILE_FAILED",
                        $"Tile download failed with status {response.StatusCode}"));
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return new Result<byte[], Error>.Success(bytes);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error downloading AgroMonitoring tile");
            return new Result<byte[], Error>.Failure(
                new Error("AGROMONITORING_NETWORK_ERROR", $"Network error: {ex.Message}"));
        }
    }
}
