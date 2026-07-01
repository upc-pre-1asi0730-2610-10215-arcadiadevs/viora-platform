using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="ChillRequirementResolver"/>. The
///     resolver looks up the chill requirement for a plot: it prefers the
///     plot's <see cref="Plot.ChillRequirementOverride"/> when set, and
///     falls back to the crop-specific value in the injected
///     <see cref="ChillRequirementPolicy"/>, then to the policy's
///     <c>DefaultRequirementPortions</c>.
/// </summary>
public class ChillRequirementResolverTests
{
    private static Plot CreatePlot(string? cropType = "Coffee")
    {
        var polygon = ((Result<PolygonCoordinates, Error>.Success)PolygonCoordinates.Create(new List<GeoPoint>
        {
            ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.0m)).Value,
            ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.1m)).Value,
            ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.1m, -77.1m)).Value,
            ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.1m, -77.0m)).Value,
            ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.0m)).Value,
        })).Value;

        return ((Result<Plot, Error>.Success)Plot.Create(
            ownerUserId: 1,
            plotName: "Test plot",
            polygonCoordinates: polygon,
            areaSize: 100m,
            cropType: cropType,
            variety: null,
            location: null,
            campaign: null,
            notes: null)).Value;
    }

    private static ChillRequirementPolicy PolicyWith(
        double defaultPortions,
        IDictionary<string, double>? cropPortions = null) =>
        new(defaultPortions, cropPortions);

    [Fact]
    public void ResolveFor_PlotWithOverride_ReturnsOverride()
    {
        // GIVEN a plot with an explicit chill requirement override (40 portions)
        var plot = CreatePlot();
        plot.ConfigureChillRequirement(new ChillPortions(40.0), EChillRequirementSource.UserDeclared);

        var resolver = new ChillRequirementResolver(PolicyWith(60.0));

        // WHEN the resolver looks up the requirement
        var requirement = resolver.ResolveFor(plot);

        // THEN the override is returned (40 portions, UserDeclared source)
        Assert.Equal(40.0, requirement.Portions.Value);
        Assert.Equal(EChillRequirementSource.UserDeclared, requirement.Source);
    }

    [Fact]
    public void ResolveFor_PlotWithoutOverride_ReturnsPolicyDefault()
    {
        // GIVEN a plot with no override and a policy with a 60-portion default
        var plot = CreatePlot();
        var resolver = new ChillRequirementResolver(PolicyWith(60.0));

        // WHEN the resolver looks up the requirement
        var requirement = resolver.ResolveFor(plot);

        // THEN the policy default is returned (60 portions, NotConfigured source)
        Assert.Equal(60.0, requirement.Portions.Value);
        Assert.Equal(EChillRequirementSource.NotConfigured, requirement.Source);
    }

    [Fact]
    public void ResolveFor_NullPlot_ReturnsPolicyDefault()
    {
        // GIVEN a null plot and a policy with a 60-portion default
        var resolver = new ChillRequirementResolver(PolicyWith(60.0));

        // WHEN the resolver looks up the requirement
        var requirement = resolver.ResolveFor(null);

        // THEN the policy default is returned
        Assert.Equal(60.0, requirement.Portions.Value);
    }

    [Fact]
    public void ResolveFor_KnownCrop_ReturnsCropValue()
    {
        // GIVEN a plot with crop "Coffee" and a policy whose crop table has Coffee -> 45 portions
        var plot = CreatePlot(cropType: "Coffee");
        var policy = PolicyWith(60.0, new Dictionary<string, double> { ["coffee"] = 45.0 });

        var resolver = new ChillRequirementResolver(policy);

        // WHEN the resolver looks up the requirement
        var requirement = resolver.ResolveFor(plot);

        // THEN the crop-specific value is returned (45 portions, SystemDefault source)
        Assert.Equal(45.0, requirement.Portions.Value);
        Assert.Equal(EChillRequirementSource.SystemDefault, requirement.Source);
    }

    [Fact]
    public void ResolveFor_UnknownCrop_FallsBackToDefault()
    {
        // GIVEN a plot with crop "Mango" and a policy whose crop table has only Coffee
        var plot = CreatePlot(cropType: "Mango");
        var policy = PolicyWith(60.0, new Dictionary<string, double> { ["coffee"] = 45.0 });

        var resolver = new ChillRequirementResolver(policy);

        // WHEN the resolver looks up the requirement
        var requirement = resolver.ResolveFor(plot);

        // THEN the policy default is returned (the crop table is a miss)
        Assert.Equal(60.0, requirement.Portions.Value);
    }

    [Fact]
    public void ResolveDefault_ReturnsPolicyDefault()
    {
        // GIVEN a policy with a 60-portion default
        var resolver = new ChillRequirementResolver(PolicyWith(60.0));

        // WHEN the resolver returns the default
        var requirement = resolver.ResolveDefault();

        // THEN the policy default is returned (no plot to consult)
        Assert.Equal(60.0, requirement.Portions.Value);
    }
}
