using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;

/// <summary>
///     Represents a cultivation unit plot with geospatial polygon boundaries.
/// </summary>
/// <remarks>
///     This is the aggregate root for plot management. Use the <see cref="Create"/> factory method
///     to enforce invariants and validate input.
/// </remarks>
public class Plot : IAuditableEntity
{
    /// <summary>
    ///     Gets the unique identifier for the plot.
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    ///     Gets the owner user identifier.
    /// </summary>
    public int OwnerUserId { get; private set; }

    /// <summary>
    ///     Gets the name of the plot.
    /// </summary>
    public string PlotName { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the crop type of the plot.
    /// </summary>
    public string? CropType { get; private set; }

    /// <summary>
    ///     Gets the polygon coordinates defining the plot boundaries.
    /// </summary>
    public PolygonCoordinates PolygonCoordinates { get; private set; } = null!;

    /// <summary>
    ///     Gets the area size of the plot (in square meters or other unit).
    /// </summary>
    public decimal AreaSize { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    ///     Gets whether this plot has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    ///     Gets the AgroMonitoring polygon identifier, if registered with the external API.
    /// </summary>
    public string? AgroMonitoringPolygonId { get; private set; }

    /// <summary>
    ///     Gets the center coordinates reported by AgroMonitoring (format: "[lon, lat]").
    /// </summary>
    public string? AgroMonitoringCenter { get; private set; }

    /// <summary>
    ///     Grower- or agronomist-declared winter-chill requirement for this plot.
    ///     Null when no override has been configured and the crop-derived system default applies.
    /// </summary>
    public ChillRequirement? ChillRequirementOverride { get; private set; }

    /// <summary>
    ///     Defensive upper bound for a declared chill requirement.
    /// </summary>
    private const double MaxChillRequirementPortions = 200.0;

    /// <summary>
    ///     EF Core constructor.
    /// </summary>
    private Plot() { }

    /// <summary>
    ///     Creates a new Plot with validated invariants.
    /// </summary>
    /// <param name="ownerUserId">The owner user identifier.</param>
    /// <param name="plotName">The name of the plot.</param>
    /// <param name="polygonCoordinates">The polygon coordinates defining boundaries.</param>
    /// <param name="areaSize">The area size of the plot.</param>
    /// <returns>A Result containing the Plot if valid, or an error if validation fails.</returns>
    public static Result<Plot, Error> Create(
        int ownerUserId,
        string plotName,
        PolygonCoordinates polygonCoordinates,
        decimal areaSize)
    {
        // Validate owner user ID
        if (ownerUserId <= 0)
            return new Result<Plot, Error>.Failure(
                new Error("OWNER_REQUIRED", "Owner user ID must be provided"));

        // Validate plot name
        if (string.IsNullOrWhiteSpace(plotName))
            return new Result<Plot, Error>.Failure(
                new Error("PLOT_NAME_REQUIRED", "Plot name must not be empty"));

        if (plotName.Trim().Length > 256)
            return new Result<Plot, Error>.Failure(
                new Error("PLOT_NAME_TOO_LONG", "Plot name must not exceed 256 characters"));

        // Validate area size
        if (areaSize <= 0)
            return new Result<Plot, Error>.Failure(
                new Error("INVALID_AREA", "Area size must be positive"));

        // Validate polygon coordinates (already validated via factory, but double-check)
        if (polygonCoordinates is null)
            return new Result<Plot, Error>.Failure(
                new Error("INVALID_POLYGON", "Polygon coordinates must be provided"));

        var plot = new Plot
        {
            OwnerUserId = ownerUserId,
            PlotName = plotName.Trim(),
            PolygonCoordinates = polygonCoordinates,
            AreaSize = areaSize
        };

        return new Result<Plot, Error>.Success(plot);
    }

    /// <summary>
    ///     Stores the AgroMonitoring polygon identifier and center after external registration.
    /// </summary>
    /// <param name="polygonId">The polygon ID returned by AgroMonitoring.</param>
    /// <param name="center">The center coordinates (format: "[lon, lat]").</param>
    public void SetAgroMonitoringData(string polygonId, string center)
    {
        AgroMonitoringPolygonId = polygonId;
        AgroMonitoringCenter = center;
    }

    /// <summary>
    ///     Declares an explicit winter-chill requirement for this plot, overriding the default.
    /// </summary>
    public Plot ConfigureChillRequirement(ChillPortions portions, EChillRequirementSource source)
    {
        ArgumentNullException.ThrowIfNull(portions);

        if (source is not EChillRequirementSource.UserDeclared and not EChillRequirementSource.AgronomistValidated)
        {
            throw new ArgumentException("A configured chill requirement must be user-declared or agronomist-validated.", nameof(source));
        }

        if (portions.Value <= 0)
        {
            throw new ArgumentException("Chill requirement must be greater than zero.");
        }

        if (portions.Value > MaxChillRequirementPortions)
        {
            throw new ArgumentException($"Chill requirement cannot exceed {MaxChillRequirementPortions:F0} chill portions.");
        }

        ChillRequirementOverride = new ChillRequirement(portions, source, EChillMetricModel.Dynamic);
        return this;
    }

    /// <summary>
    ///     Clears any declared chill requirement, reverting to the crop-derived system default.
    /// </summary>
    public Plot ClearChillRequirement()
    {
        ChillRequirementOverride = null;
        return this;
    }
}
