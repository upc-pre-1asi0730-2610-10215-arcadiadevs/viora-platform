using System.Reflection;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     A3 — <c>HasRelatedOperationalRecordsAsync</c> intra-BC completion tests.
///     Covers spec acceptance scenarios S3.1..S3.9 (engram #43).
///     Uses the EF Core InMemory provider with a unique database name per test
///     for state isolation (Phase 1 test pattern).
/// </summary>
public class PlotRepositoryTests
{
    // ---------- helpers ----------

    private static IClock FixedClock(DateTime? at = null)
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(at ?? new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc));
        return clock;
    }

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

    private static Plot CreatePlot(int ownerUserId = 1) =>
        ((Result<Plot, Error>.Success)Plot.Create(
            ownerUserId,
            $"Plot-{Guid.NewGuid():N}",
            MakePolygon(),
            100m,
            "Coffee",
            "Arabica",
            "Lima",
            "2026",
            "test notes")).Value;

    private static IoTDevice CreateDevice(long plotId, IClock clock, string? name = null) =>
        ((Result<IoTDevice, Error>.Success)IoTDevice.Create(
            plotId,
            name ?? $"Sensor-{Guid.NewGuid():N}",
            clock)).Value;

    private static AgronomicStatistic CreateStatistic(long plotId, IClock clock) =>
        ((Result<AgronomicStatistic, Error>.Success)AgronomicStatistic.Create(
            userId: 1L,
            plotId: plotId,
            measurementDate: new DateTimeOffset(clock.UtcNow, TimeSpan.Zero),
            ndviValue: 0.5,
            chillPortions: 10.0,
            chillHours: 50.0,
            chillModelState: ChillModelState.Empty())).Value;

    private static DynamicNutritionPlan CreateActivePlan(int plotId, IClock clock) =>
        DynamicNutritionPlan.Recommend(
            userId: 1,
            plotId: plotId,
            inputRecommendations: new List<NutritionInputRecommendation>
            {
                new("NPK 20-20-20", "Base fertilization", 50.0, "kg/ha", ENutritionInputStatus.Recommended)
            },
            applicationWindow: new NutritionApplicationWindow(
                new DateTimeOffset(clock.UtcNow, TimeSpan.Zero).AddDays(1),
                new DateTimeOffset(clock.UtcNow, TimeSpan.Zero).AddDays(7)),
            rationale: new PlanRationale(
                "Test rationale summary",
                EClimateRiskLevel.High,
                new NdviValue(0.45),
                1.5),
            generatedDate: new DateTimeOffset(clock.UtcNow, TimeSpan.Zero));

    private static DbContextOptions<AppDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"PlotRepositoryTests-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static AppDbContext NewContext() => new(NewInMemoryOptions());

    private static async Task<int> SeedPlotAsync(AppDbContext context, int ownerUserId = 1)
    {
        var plot = CreatePlot(ownerUserId);
        context.Plots.Add(plot);
        await context.SaveChangesAsync();
        return plot.Id;
    }

    // ---------- S3.1 ----------

    [Fact]
    public async Task HasRelatedOperationalRecordsAsync_PlotWithOneIoTDevice_ReturnsTrue()
    {
        // Arrange
        var clock = FixedClock();
        await using var context = NewContext();
        var plotId = await SeedPlotAsync(context);
        context.Set<IoTDevice>().Add(CreateDevice(plotId, clock));
        await context.SaveChangesAsync();
        var repository = new PlotRepository(context);

        // Act
        var result = await repository.HasRelatedOperationalRecordsAsync(plotId);

        // Assert
        Assert.True(result);
    }

    // ---------- S3.2 ----------

    [Fact]
    public async Task HasRelatedOperationalRecordsAsync_PlotWithOneDynamicNutritionPlan_ReturnsTrue()
    {
        // Arrange
        var clock = FixedClock();
        await using var context = NewContext();
        var plotId = await SeedPlotAsync(context);
        context.DynamicNutritionPlans.Add(CreateActivePlan(plotId, clock));
        await context.SaveChangesAsync();
        var repository = new PlotRepository(context);

        // Act
        var result = await repository.HasRelatedOperationalRecordsAsync(plotId);

        // Assert
        Assert.True(result);
    }

    // ---------- S3.3 ----------

    [Fact]
    public async Task HasRelatedOperationalRecordsAsync_PlotWithOneAgronomicStatistic_ReturnsTrue()
    {
        // Arrange
        var clock = FixedClock();
        await using var context = NewContext();
        var plotId = await SeedPlotAsync(context);
        context.Set<AgronomicStatistic>().Add(CreateStatistic(plotId, clock));
        await context.SaveChangesAsync();
        var repository = new PlotRepository(context);

        // Act
        var result = await repository.HasRelatedOperationalRecordsAsync(plotId);

        // Assert
        Assert.True(result);
    }

    // ---------- S3.4 ----------

    [Fact]
    public async Task HasRelatedOperationalRecordsAsync_PlotWithNoRecords_ReturnsFalse()
    {
        // Arrange
        await using var context = NewContext();
        var plotId = await SeedPlotAsync(context);
        var repository = new PlotRepository(context);

        // Act
        var result = await repository.HasRelatedOperationalRecordsAsync(plotId);

        // Assert
        Assert.False(result);
    }

    // ---------- S3.5 ----------

    [Fact]
    public async Task HasRelatedOperationalRecordsAsync_PlotWithSupersededPlanAndInactiveDevice_ReturnsTrue()
    {
        // Arrange
        var clock = FixedClock();
        await using var context = NewContext();
        var plotId = await SeedPlotAsync(context);

        // Device: Pending -> Active -> Inactive (terminal)
        var device = CreateDevice(plotId, clock);
        _ = device.Activate();
        _ = device.Deactivate();
        context.Set<IoTDevice>().Add(device);

        // Plan: Active -> Superseded (terminal). Recommend() yields Active;
        // we flip it manually via the public domain method.
        var plan = CreateActivePlan(plotId, clock);
        plan.Supersede();
        context.DynamicNutritionPlans.Add(plan);

        await context.SaveChangesAsync();
        var repository = new PlotRepository(context);

        // Act
        var result = await repository.HasRelatedOperationalRecordsAsync(plotId);

        // Assert — defensive: include terminal states (spec S3.5)
        Assert.True(result);
    }

    // ---------- S3.6 ----------

    [Fact]
    public async Task HasRelatedOperationalRecordsAsync_PlotWithRecordsForDifferentPlot_ReturnsFalse()
    {
        // Arrange — seed TWO plots; put records on plot #2; query for plot #1.
        var clock = FixedClock();
        await using var context = NewContext();
        var plot1Id = await SeedPlotAsync(context, ownerUserId: 1);
        var plot2 = CreatePlot(ownerUserId: 1);
        context.Plots.Add(plot2);
        await context.SaveChangesAsync();
        var plot2Id = plot2.Id;

        context.Set<IoTDevice>().Add(CreateDevice(plot2Id, clock));
        context.DynamicNutritionPlans.Add(CreateActivePlan(plot2Id, clock));
        context.Set<AgronomicStatistic>().Add(CreateStatistic(plot2Id, clock));
        await context.SaveChangesAsync();

        var repository = new PlotRepository(context);

        // Act — query for plot #1 with no records
        var result = await repository.HasRelatedOperationalRecordsAsync(plot1Id);

        // Assert — ownership scoping: plot #1's records count is zero
        Assert.False(result);
    }

    // ---------- S3.7 ----------

    [Fact]
    public async Task HasRelatedOperationalRecordsAsync_PlotIdNotFound_ReturnsFalse()
    {
        // Arrange
        await using var context = NewContext();
        var repository = new PlotRepository(context);

        // Act — query a plot id that does not exist
        var result = await repository.HasRelatedOperationalRecordsAsync(plotId: 99_999);

        // Assert
        Assert.False(result);
    }

    // ---------- S3.8 ----------

    [Fact]
    public async Task HasRelatedOperationalRecordsAsync_CancellationTokenPropagatesToQuery()
    {
        // Arrange
        var clock = FixedClock();
        await using var context = NewContext();
        var plotId = await SeedPlotAsync(context);
        context.Set<IoTDevice>().Add(CreateDevice(plotId, clock));
        await context.SaveChangesAsync();
        var repository = new PlotRepository(context);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act + Assert — pre-cancelled token must surface as OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.HasRelatedOperationalRecordsAsync(plotId, cts.Token));
    }

    // ---------- S3.9 ----------

    [Fact]
    public void HasRelatedOperationalRecordsAsync_CrossBcDocumentedLimitation_IsDocumentedInXmlDoc()
    {
        // Arrange — pull the method's XML doc and look for the deferred-resolution TODOs
        var method = typeof(PlotRepository).GetMethod(
            nameof(PlotRepository.HasRelatedOperationalRecordsAsync),
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Method not found on PlotRepository.");

        // Act
        var xmlDoc = method.GetXmlDocumentation()
            ?? throw new InvalidOperationException("Method has no XML documentation.");

        // Assert — the spec acceptance gate for S3.9: the method's xml-doc must
        // name `IAgronomicContextFacade` AND `SHARED-015` as the deferred
        // resolution for the cross-BC checks (Decision #2 in engram #42).
        Assert.Contains("IAgronomicContextFacade", xmlDoc, StringComparison.Ordinal);
        Assert.Contains("SHARED-015", xmlDoc, StringComparison.Ordinal);
    }
}

/// <summary>
///     Local XML-doc helper (mirrors System.Xml.XmlDocument-based reflection
///     but kept here to avoid an extra package reference).
/// </summary>
internal static class MemberInfoXmlDocExtensions
{
    public static string? GetXmlDocumentation(this MemberInfo member)
    {
        var assembly = member.Module.Assembly;
        var assemblyName = assembly.GetName().Name;
        if (assemblyName is null) return null;

        // The test project does not generate a .xml for the SUT; look next to
        // the SUT assembly (the runtime location of ArcadiaDevs.Viora.Platform.dll).
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? AppContext.BaseDirectory;
        var xmlFile = $"{assemblyName}.xml";
        var xmlPath = Path.Combine(assemblyDir, xmlFile);
        if (!File.Exists(xmlPath))
        {
            // Fallback — the test runner copies the SUT next to the test DLL.
            xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (!File.Exists(xmlPath)) return null;
        }

        using var stream = File.OpenRead(xmlPath);
        var document = System.Xml.Linq.XDocument.Load(stream);
        var memberName = BuildMemberName(member);
        var memberElement = document
            .Descendants("member")
            .FirstOrDefault(x => x.Attribute("name")?.Value == memberName);
        if (memberElement is null) return null;

        // Spec S3.9 asserts that the method's XML doc contains the deferred-resolution
        // markers in the REMARKS block. Combine <summary> + <remarks> so a single
        // substring search covers both.
        var summary = memberElement.Element("summary")?.Value ?? string.Empty;
        var remarks = memberElement.Element("remarks")?.Value ?? string.Empty;
        return string.IsNullOrEmpty(remarks)
            ? summary.Trim()
            : (summary + "\n" + remarks).Trim();
    }

    private static string BuildMemberName(MemberInfo member)
    {
        var prefix = member switch
        {
            Type t => "T",
            _ => member.MemberType.ToString()[0].ToString()
        };

        var declaring = member.DeclaringType?.FullName ?? member.Name;
        if (member is Type type && type.IsGenericType)
        {
            // T:Namespace.Type`1
            return $"{prefix}:{type.FullName}";
        }

        if (member is MethodInfo method)
        {
            var parameters = method.GetParameters();
            var paramTypes = parameters.Length == 0
                ? string.Empty
                : "(" + string.Join(",", parameters.Select(p => p.ParameterType.FullName)) + ")";
            return $"{prefix}:{declaring}.{method.Name}{paramTypes}";
        }

        return $"{prefix}:{declaring}.{member.Name}";
    }
}
