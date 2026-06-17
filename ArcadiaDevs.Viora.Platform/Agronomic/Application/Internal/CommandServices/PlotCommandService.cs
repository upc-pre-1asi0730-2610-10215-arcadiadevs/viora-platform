using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.ExternalServices;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

/// <summary>
///     Handles plot commands and coordinates persistence through Shared.
/// </summary>
public class PlotCommandService(
    IPlotRepository plotRepository,
    IUnitOfWork unitOfWork,
    AgroMonitoringApiClient agroMonitoringClient,
    ILogger<PlotCommandService> logger,
    ChillRequirementResolver chillRequirementResolver) : IPlotCommandService
{
    /// <inheritdoc />
    public async Task<Result<Plot, Error>> Handle(
        CreatePlotCommand command,
        CancellationToken cancellationToken = default)
    {
        var polygonResult = PolygonCoordinates.Create(command.PolygonCoordinates);
        if (polygonResult is Result<PolygonCoordinates, Error>.Failure polygonFailure)
            return new Result<Plot, Error>.Failure(polygonFailure.Error);

        var polygon = ((Result<PolygonCoordinates, Error>.Success)polygonResult).Value;
        var plotResult = Plot.Create(
            command.OwnerUserId,
            command.PlotName,
            polygon,
            command.AreaSize);

        if (plotResult is Result<Plot, Error>.Failure plotFailure)
            return new Result<Plot, Error>.Failure(plotFailure.Error);

        var plot = ((Result<Plot, Error>.Success)plotResult).Value;

        // Register polygon with AgroMonitoring API (best-effort; failure is logged, not fatal).
        var polygonResponse = await agroMonitoringClient.CreatePolygonAsync(
            command.PlotName,
            command.PolygonCoordinates,
            cancellationToken);

        if (polygonResponse is Result<AgroMonitoringPolygonResponse, Error>.Success polygonSuccess)
        {
            var polygonData = polygonSuccess.Value;
            var center = polygonData.Center.Length >= 2
                ? $"[{polygonData.Center[0]}, {polygonData.Center[1]}]"
                : "[]";
            plot.SetAgroMonitoringData(polygonData.Id, center);
        }
        else
        {
            logger.LogWarning(
                "Failed to register polygon with AgroMonitoring for plot '{PlotName}'. " +
                "NDVI and temperature data will not be available.",
                command.PlotName);
        }

        await plotRepository.AddAsync(plot, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        return new Result<Plot, Error>.Success(plot);
    }

    /// <inheritdoc />
    public async Task<Result<ChillRequirement, Error>> Handle(
        ConfigureChillRequirementCommand command,
        CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(command.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
        {
            return new Result<ChillRequirement, Error>.Failure(new Error("PLOT_NOT_FOUND", "Plot not found."));
        }

        if (plot.OwnerUserId != command.UserId)
        {
            return new Result<ChillRequirement, Error>.Failure(new Error("UNAUTHORIZED_ACCESS", "User does not own the plot."));
        }

        var portions = new ChillPortions(command.ChillRequirementPortions);
        plot.ConfigureChillRequirement(portions, EChillRequirementSource.UserDeclared);

        plotRepository.Update(plot);
        await unitOfWork.CompleteAsync(cancellationToken);

        return new Result<ChillRequirement, Error>.Success(chillRequirementResolver.ResolveFor(plot));
    }

    /// <inheritdoc />
    public async Task<Result<ChillRequirement, Error>> Handle(
        ResetChillRequirementCommand command,
        CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(command.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
        {
            return new Result<ChillRequirement, Error>.Failure(new Error("PLOT_NOT_FOUND", "Plot not found."));
        }

        if (plot.OwnerUserId != command.UserId)
        {
            return new Result<ChillRequirement, Error>.Failure(new Error("UNAUTHORIZED_ACCESS", "User does not own the plot."));
        }

        plot.ClearChillRequirement();

        plotRepository.Update(plot);
        await unitOfWork.CompleteAsync(cancellationToken);

        return new Result<ChillRequirement, Error>.Success(chillRequirementResolver.ResolveFor(plot));
    }
}
