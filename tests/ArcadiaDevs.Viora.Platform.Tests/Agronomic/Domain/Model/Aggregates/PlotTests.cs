using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Aggregates;

public class PlotTests
{
    private static GeoPoint MakePoint(decimal lat, decimal lon) =>
        ((Result<GeoPoint, Error>.Success)GeoPoint.Create(lat, lon)).Value;

    private static PolygonCoordinates MakePolygon()
    {
        var points = new List<GeoPoint>
        {
            MakePoint(-12.0m, -77.0m),
            MakePoint(-12.0m, -77.1m),
            MakePoint(-12.1m, -77.1m),
            MakePoint(-12.1m, -77.0m),
            MakePoint(-12.0m, -77.0m)
        };
        return ((Result<PolygonCoordinates, Error>.Success)PolygonCoordinates.Create(points)).Value;
    }

    private static Plot CreateValidPlot() =>
        ((Result<Plot, Error>.Success)Plot.Create(1, "Test Plot", MakePolygon(), 100m, "Coffee", "Arabica", "Lima", "2026", "notes")).Value;

    [Fact]
    public void UpdateInformation_WithInvalidName_ReturnsFailureAndDoesNotMutate()
    {
        // Arrange
        var plot = CreateValidPlot();
        var originalName = plot.PlotName;
        var originalCropType = plot.CropType;
        var originalVariety = plot.Variety;
        var originalLocation = plot.Location;
        var originalCampaign = plot.Campaign;
        var originalNotes = plot.Notes;

        // Act — empty name should fail validation
        var result = plot.UpdateInformation("", "NewCrop", "NewVariety", "NewLocation", "NewCampaign", "NewNotes");

        // Assert — result is Failure
        Assert.True(result.IsFailure);
        var error = ((Result<Unit, Error>.Failure)result).Error;
        Assert.Equal("PLOT_NAME_REQUIRED", error.Code);

        // Assert — state is unchanged (mutation did not occur)
        Assert.Equal(originalName, plot.PlotName);
        Assert.Equal(originalCropType, plot.CropType);
        Assert.Equal(originalVariety, plot.Variety);
        Assert.Equal(originalLocation, plot.Location);
        Assert.Equal(originalCampaign, plot.Campaign);
        Assert.Equal(originalNotes, plot.Notes);
    }

    [Fact]
    public void UpdateInformation_WithValidInputs_ReturnsSuccessAndMutatesState()
    {
        // Arrange
        var plot = CreateValidPlot();

        // Act
        var result = plot.UpdateInformation("Updated Name", "Wheat", "Durum", "Cusco", "2027", "updated notes");

        // Assert — result is Success
        Assert.True(result.IsSuccess);

        // Assert — state reflects new values
        Assert.Equal("Updated Name", plot.PlotName);
        Assert.Equal("Wheat", plot.CropType);
        Assert.Equal("Durum", plot.Variety);
        Assert.Equal("Cusco", plot.Location);
        Assert.Equal("2027", plot.Campaign);
        Assert.Equal("updated notes", plot.Notes);
    }

    [Fact]
    public void UpdateInformation_WithWhitespaceOnlyName_ReturnsFailureAndDoesNotMutate()
    {
        // Arrange
        var plot = CreateValidPlot();
        var originalName = plot.PlotName;

        // Act
        var result = plot.UpdateInformation("   ", "Crop", null, null, null, null);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(originalName, plot.PlotName);
    }
}
