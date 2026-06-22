using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
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
    private readonly PlotDeletionPolicy _plotDeletionPolicy = new();
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
            command.AreaSize,
            command.CropType,
            command.Variety,
            command.Location,
            command.Campaign,
            command.Notes);

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

    /// <inheritdoc />
    public async Task<Result<Plot, Error>> Handle(
        UpdatePlotCommand command,
        CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(command.PlotId, cancellationToken);
        if (plot is null)
        {
            return new Result<Plot, Error>.Failure(new Error("PLOT_NOT_FOUND", $"Plot {command.PlotId} not found."));
        }

        if (!plot.IsActive)
        {
            return new Result<Plot, Error>.Failure(new Error("PLOT_INACTIVE", "Only active plots can be updated."));
        }

        if (!string.IsNullOrWhiteSpace(command.Name) && 
            await plotRepository.ExistsByNameAndOwnerUserIdAndIdIsNotAsync(command.Name, plot.OwnerUserId, plot.Id, cancellationToken))
        {
            return new Result<Plot, Error>.Failure(new Error("PLOT_CONFLICT", "A plot with the same name already exists for this user."));
        }

        var updatedName = command.Name ?? plot.PlotName;
        var updatedCropType = command.CropType ?? plot.CropType;
        var updatedVariety = command.Variety ?? plot.Variety;
        var updatedLocation = command.Location ?? plot.Location;
        var updatedCampaign = command.Campaign ?? plot.Campaign;
        var updatedNotes = command.Notes ?? plot.Notes;

        var infoResult = plot.UpdateInformation(
            updatedName, updatedCropType, updatedVariety, updatedLocation, updatedCampaign, updatedNotes);

        if (infoResult is Result<Plot, Error>.Failure infoFailure)
        {
            return infoFailure;
        }

        if (command.PolygonCoordinates != null)
        {
            var polygonResult = PolygonCoordinates.Create(command.PolygonCoordinates);
            if (polygonResult is Result<PolygonCoordinates, Error>.Failure polygonFailure)
            {
                return new Result<Plot, Error>.Failure(polygonFailure.Error);
            }
            
            var newPolygon = ((Result<PolygonCoordinates, Error>.Success)polygonResult).Value;
            // For simplicity in C# we will just recalculate area size based on the previous or pass a generic one since AreaSize calculation is not fully implemented in PolygonCoordinates C#
            // Wait, in C# the area size was passed in the CreatePlotCommand but not UpdatePlotCommand.
            // Let's preserve the existing area size if we just update the boundary without a new area size, or require it if we were to be exact.
            plot.UpdateBoundary(newPolygon, plot.AreaSize);
        }

        plotRepository.Update(plot);
        await unitOfWork.CompleteAsync(cancellationToken);

        return new Result<Plot, Error>.Success(plot);
    }

    /// <inheritdoc />
    public async Task<Result<string, Error>> Handle(
        DeletePlotCommand command,
        CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(command.PlotId, cancellationToken);
        if (plot is null)
        {
            return new Result<string, Error>.Failure(new Error("PLOT_NOT_FOUND", $"Plot {command.PlotId} not found."));
        }

        if (!_plotDeletionPolicy.CanDelete(plot))
        {
            return new Result<string, Error>.Failure(
                new Error("DELETE_ACTIVE_PLOT", _plotDeletionPolicy.ExplainDeletionRejection(plot)));
        }

        var hasRelatedOperationalRecords = await plotRepository.HasRelatedOperationalRecordsAsync(plot.Id, cancellationToken);

        if (_plotDeletionPolicy.RequiresLogicalDeletion(hasRelatedOperationalRecords))
        {
            plot.Deactivate();
            plotRepository.Update(plot);
        }
        else
        {
            plotRepository.Remove(plot);
        }

        await unitOfWork.CompleteAsync(cancellationToken);

        return new Result<string, Error>.Success("Plot deleted successfully.");
    }
}
