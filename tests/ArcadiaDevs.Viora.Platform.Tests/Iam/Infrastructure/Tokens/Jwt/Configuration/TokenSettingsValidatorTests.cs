using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using Microsoft.Extensions.Options;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Infrastructure.Tokens.Jwt.Configuration;

public class TokenSettingsValidatorTests
{
    private readonly TokenSettingsValidator _sut = new();

    [Fact]
    public void Validate_WithPlaceholderSecret_ReturnsFail()
    {
        // Arrange
        var settings = new TokenSettings { Secret = "DEV-ONLY-PLEASE-CHANGE-ME" };
        var options = Options.Create(settings);

        // Act
        var result = _sut.Validate("Jwt", options.Value);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureMessage);
        Assert.Contains("placeholder", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithShortSecret_ReturnsFail()
    {
        // Arrange — 16 bytes is less than the 32-byte minimum
        var settings = new TokenSettings { Secret = "short-secret-16b" };
        var options = Options.Create(settings);

        // Act
        var result = _sut.Validate("Jwt", options.Value);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureMessage);
        Assert.Contains("32", result.FailureMessage);
    }

    [Fact]
    public void Validate_WithEmptySecret_ReturnsFail()
    {
        // Arrange
        var settings = new TokenSettings { Secret = "" };
        var options = Options.Create(settings);

        // Act
        var result = _sut.Validate("Jwt", options.Value);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureMessage);
    }

    [Fact]
    public void Validate_WithValidSecret_ReturnsSuccess()
    {
        // Arrange — 64-byte random string
        var settings = new TokenSettings
        {
            Secret = "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2"
        };
        var options = Options.Create(settings);

        // Act
        var result = _sut.Validate("Jwt", options.Value);

        // Assert
        Assert.True(result.Succeeded);
    }
}
