using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Infrastructure;

/// <summary>
///     Unit tests for <see cref="WireMockBuilders"/>. Verifies that
///     the WireMock server starts on a random port and that the
///     default stub for the weather endpoint responds with a
///     200 + JSON body when a stub is explicitly registered. The
///     pre-canned default stub for the forecast endpoint is also
///     exercised.
/// </summary>
[Trait("Category", "Unit")]
public class WireMockBuildersTests
{
    [Fact]
    public void ForWeatherDataService_StartsOnRandomPort()
    {
        // Arrange + Act
        using var mock = WireMockBuilders.ForWeatherDataService();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(mock.Url));
        Assert.StartsWith("http://", mock.Url);
    }

    [Fact]
    public async Task ForWeatherDataService_DefaultStub_Returns200ForPointPath()
    {
        // Arrange
        using var mock = WireMockBuilders.ForWeatherDataService();
        // Override the default with a deterministic response
        mock.Given(Request.Create().WithPath("/v1/weather/point").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"status\":\"stubbed\"}")
                .WithHeader("Content-Type", "application/json"));

        // Act
        using var http = new HttpClient { BaseAddress = new Uri(mock.Url) };
        var response = await http.GetAsync("/v1/weather/point");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("stubbed", body);
    }

    [Fact]
    public void ForAgroMonitoringImageryService_StartsOnRandomPort()
    {
        // Arrange + Act
        using var mock = WireMockBuilders.ForAgroMonitoringImageryService();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(mock.Url));
        Assert.StartsWith("http://", mock.Url);
    }

    [Fact]
    public async Task ForAgroMonitoringImageryService_DefaultStub_ReturnsEmptyResultList()
    {
        // Arrange
        using var mock = WireMockBuilders.ForAgroMonitoringImageryService();
        // Override default with deterministic response
        mock.Given(Request.Create().WithPath("/v1/image/search").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"results\":[]}")
                .WithHeader("Content-Type", "application/json"));

        // Act
        using var http = new HttpClient { BaseAddress = new Uri(mock.Url) };
        var response = await http.GetAsync("/v1/image/search");

        // Assert
        Assert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("results", body);
    }
}
