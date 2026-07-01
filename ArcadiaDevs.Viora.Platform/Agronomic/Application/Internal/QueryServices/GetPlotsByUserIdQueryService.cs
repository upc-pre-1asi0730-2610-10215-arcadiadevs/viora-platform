using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class GetPlotsByUserIdQueryService(
    IPlotRepository plotRepository,
    ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock) : IGetPlotsByUserIdQueryService
{
    public async Task<Result<IEnumerable<PlotResource>, Error>> Handle(GetPlotsByUserIdQuery query, CancellationToken cancellationToken = default)
    {
        var plots = await plotRepository.ListAsync(cancellationToken);
        var userPlots = plots.Where(p => p.OwnerUserId == query.UserId && !p.IsDeleted)
                             .Select(p => MapToResource(p, query.IncludeCurrentImagery, clock))
                             .ToList();

        return new Result<IEnumerable<PlotResource>, Error>.Success(userPlots);
    }

    private static PlotResource MapToResource(Plot plot, bool includeCurrentImagery, ArcadiaDevs.Viora.Platform.Shared.Domain.IClock clock)
    {
        var polygon = plot.PolygonCoordinates.Points
            .Select(p => (IEnumerable<double>)new double[] { (double)p.Longitude, (double)p.Latitude })
            .ToList();

        var now = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero);

        CurrentImageryResource? imagery = null;
        if (includeCurrentImagery)
        {
            imagery = new CurrentImageryResource(
                "img-" + plot.Id,
                plot.Id,
                "https://satellite.viora.local/tiles/" + plot.Id,
                now.AddDays(-1),
                0.65,
                0.05
            );
        }

        return new PlotResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            polygon,
            plot.AreaSize,
            plot.CreatedAt ?? now,
            plot.CropType,
            plot.Variety,
            plot.Location,
            plot.Campaign,
            plot.Notes,
            plot.IsActive ? "active" : "inactive",
            "Healthy",
            "Low",
            imagery
        );
    }
}
