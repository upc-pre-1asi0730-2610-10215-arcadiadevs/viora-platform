using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;

/// <summary>
///     Application contract for plot commands.
/// </summary>
public interface IPlotCommandService
{
    /// <summary>
    ///     Creates and persists a plot.
    /// </summary>
    Task<Result<Plot, Error>> Handle(
        CreatePlotCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Configures the chill requirement for a plot.
    /// </summary>
    Task<Result<ChillRequirement, Error>> Handle(
        ConfigureChillRequirementCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Handles the ResetChillRequirement command.
    /// </summary>
    Task<Result<ChillRequirement, Error>> Handle(
        ResetChillRequirementCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Handles the UpdatePlot command.
    /// </summary>
    Task<Result<Plot, Error>> Handle(
        UpdatePlotCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Handles the DeletePlot command.
    /// </summary>
    Task<Result<string, Error>> Handle(
        DeletePlotCommand command,
        CancellationToken cancellationToken = default);
}
