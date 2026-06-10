using ArcadiaDevs.Viora.Platform.Agronomic.Application.Commands.CreatePlot;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.DTOs.Requests;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.Assemblers;

/// <summary>
///     Mapper to convert CreatePlotRequest DTO to CreatePlotCommand.
/// </summary>
public static class CreatePlotRequestToCommandMapper
{
    /// <summary>
    ///     Maps a CreatePlotRequest to a CreatePlotCommand.
    /// </summary>
    /// <param name="request">The request DTO.</param>
    /// <returns>The mapped command.</returns>
    public static CreatePlotCommand ToCommand(this CreatePlotRequest request)
    {
        var geoPoints = request.PolygonCoordinates
            .Select(dto => new GeoPoint { Latitude = dto.Latitude, Longitude = dto.Longitude })
            .ToList();

        return new CreatePlotCommand(
            request.OwnerUserId,
            request.PlotName,
            geoPoints,
            request.AreaSize);
    }
}