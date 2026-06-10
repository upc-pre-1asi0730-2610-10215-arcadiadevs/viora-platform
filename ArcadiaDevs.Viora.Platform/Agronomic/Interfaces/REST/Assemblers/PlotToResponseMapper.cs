using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates.Plot;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.DTOs.Responses;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.REST.Assemblers;

/// <summary>
///     Mapper to convert Plot aggregate to CreatePlotResponse DTO.
/// </summary>
public static class PlotToResponseMapper
{
    /// <summary>
    ///     Maps a Plot aggregate to a CreatePlotResponse DTO.
    /// </summary>
    /// <param name="plot">The plot aggregate.</param>
    /// <returns>The mapped response DTO.</returns>
    public static CreatePlotResponse ToResponse(this Plot plot)
    {
        var geoPointDtos = plot.PolygonCoordinates.Points
            .Select(p => new GeoPointDto(p.Latitude, p.Longitude))
            .ToList();

        return new CreatePlotResponse(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            geoPointDtos,
            plot.AreaSize,
            plot.CreatedAt ?? DateTimeOffset.UtcNow);
    }
}