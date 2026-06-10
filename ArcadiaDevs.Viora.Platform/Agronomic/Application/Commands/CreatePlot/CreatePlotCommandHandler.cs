using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates.Plot;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Cortex.Mediator.Commands;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Commands.CreatePlot;

/// <summary>
///     Handles the CreatePlotCommand by validating invariants, persisting the plot,
///     and returning Result&lt;Plot, Error&gt;.
/// </summary>
public class CreatePlotCommandHandler : ICommandHandler<CreatePlotCommand, Result<Plot, Error>>
{
    private readonly IPlotRepository _plotRepository;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CreatePlotCommandHandler"/> class.
    /// </summary>
    /// <param name="plotRepository">The plot repository.</param>
    public CreatePlotCommandHandler(IPlotRepository plotRepository)
    {
        _plotRepository = plotRepository ?? throw new ArgumentNullException(nameof(plotRepository));
    }

    /// <inheritdoc />
    public async Task<Result<Plot, Error>> Handle(
        CreatePlotCommand command,
        CancellationToken cancellationToken)
    {
        // Validate polygon coordinates via factory
        var polygonResult = PolygonCoordinates.Create(command.PolygonCoordinates);
        if (polygonResult is Result<PolygonCoordinates, Error>.Failure polygonFailure)
            return new Result<Plot, Error>.Failure(polygonFailure.Error);

        var polygon = ((Result<PolygonCoordinates, Error>.Success)polygonResult).Value;

        // Create plot aggregate with invariants
        var plotResult = Plot.Create(
            command.OwnerUserId,
            command.PlotName,
            polygon,
            command.AreaSize);

        if (plotResult is Result<Plot, Error>.Failure plotFailure)
            return new Result<Plot, Error>.Failure(plotFailure.Error);

        var plot = ((Result<Plot, Error>.Success)plotResult).Value;

        // Persist
        await _plotRepository.AddAsync(plot, cancellationToken);

        return new Result<Plot, Error>.Success(plot);
    }
}