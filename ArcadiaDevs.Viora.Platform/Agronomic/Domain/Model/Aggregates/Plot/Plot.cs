using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates.Plot;

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
            PlotName = plotName,
            PolygonCoordinates = polygonCoordinates,
            AreaSize = areaSize
        };

        return new Result<Plot, Error>.Success(plot);
    }
}