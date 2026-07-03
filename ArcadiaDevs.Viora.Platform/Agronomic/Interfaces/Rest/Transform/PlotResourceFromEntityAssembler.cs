using System;
using System.Collections.Generic;
using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Domain;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

public static class PlotResourceFromEntityAssembler
{
    public static PlotResource ToResource(this Plot plot, IClock clock)
    {
        var polygon = plot.PolygonCoordinates.Points
            .Select(p => (IEnumerable<double>)new double[] { (double)p.Longitude, (double)p.Latitude })
            .ToList();

        return new PlotResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            polygon,
            plot.AreaSize,
            plot.CreatedAt ?? new DateTimeOffset(clock.UtcNow, TimeSpan.Zero),
            plot.CropType,
            plot.Variety,
            plot.Location,
            plot.Campaign,
            plot.Notes,
            plot.IsActive ? "active" : "inactive"
        );
    }

    public static CreatedPlotResource ToCreatedResource(this Plot plot)
    {
        var polygon = plot.PolygonCoordinates.Points
            .Select(p => (IEnumerable<double>)new double[] { (double)p.Longitude, (double)p.Latitude })
            .ToList();

        return new CreatedPlotResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            polygon,
            plot.AreaSize,
            plot.CropType,
            plot.Variety,
            plot.Location,
            plot.Campaign,
            plot.Notes,
            plot.IsActive ? "active" : "inactive",
            "https://climate.viora.local/plot/" + plot.Id,
            "https://satellite.viora.local/ndvi/" + plot.Id,
            "0 devices"
        );
    }
}
