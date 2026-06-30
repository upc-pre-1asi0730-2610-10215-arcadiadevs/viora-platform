using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices.Configuration;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Infrastructure.ExternalServices.Configuration;

/// <summary>
/// Verifies that <see cref="AgroMonitoringWeatherOptionsValidator"/> enforces a non-empty
/// API key at startup (CC-5 fail-fast in all environments).
/// </summary>
/// <remarks>
/// AGRO-003: the weather provider is sole AgroMonitoring. The application must refuse
/// to start when the key is missing, empty, or whitespace-only — the only fail-fast
/// guarantee we have that the production path never falls back to fabricated data.
/// </remarks>
public class AgroMonitoringWeatherOptionsValidatorTests
{
    private readonly AgroMonitoringWeatherOptionsValidator _sut = new();

    [Fact]
    public void Validate_WithMissingApiKey_ReturnsFail()
    {
        // Arrange — ApiKey is null (the default of a fresh options instance)
        var options = Options.Create(new AgroMonitoringWeatherOptions { ApiKey = null! });

        // Act
        var result = _sut.Validate("Agronomic:Weather:AgroMonitoring", options.Value);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureMessage);
        Assert.Contains("ApiKey", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithEmptyApiKey_ReturnsFail()
    {
        // Arrange
        var options = Options.Create(new AgroMonitoringWeatherOptions { ApiKey = string.Empty });

        // Act
        var result = _sut.Validate("Agronomic:Weather:AgroMonitoring", options.Value);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureMessage);
        Assert.Contains("ApiKey", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithWhitespaceApiKey_ReturnsFail()
    {
        // Arrange
        var options = Options.Create(new AgroMonitoringWeatherOptions { ApiKey = "   " });

        // Act
        var result = _sut.Validate("Agronomic:Weather:AgroMonitoring", options.Value);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureMessage);
    }

    [Fact]
    public void Validate_WithValidApiKey_ReturnsSuccess()
    {
        // Arrange
        var options = Options.Create(new AgroMonitoringWeatherOptions
        {
            ApiKey = "a-real-agromonitoring-api-key"
        });

        // Act
        var result = _sut.Validate("Agronomic:Weather:AgroMonitoring", options.Value);

        // Assert
        Assert.True(result.Succeeded);
    }
}
