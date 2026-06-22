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
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class GetPlotByIdQueryService(IPlotRepository plotRepository) : IGetPlotByIdQueryService
{
    public async Task<Result<PlotResource, Error>> Handle(GetPlotByIdQuery query, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
            return new Result<PlotResource, Error>.Failure(AgronomicErrors.PlotNotFound);

        var polygon = plot.PolygonCoordinates.Points
            .Select(p => (IEnumerable<double>)new double[] { (double)p.Longitude, (double)p.Latitude })
            .ToList();

        var resource = new PlotResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            polygon,
            plot.AreaSize,
            plot.CreatedAt ?? DateTimeOffset.UtcNow,
            plot.CropType,
            plot.Variety,
            plot.Location,
            plot.Campaign,
            plot.Notes,
            plot.IsActive ? "active" : "inactive",
            "Good",
            "Low",
            null
        );

        return new Result<PlotResource, Error>.Success(resource);
    }
}
