using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;

public class GetPlotWeatherForecastQueryService(IPlotRepository plotRepository) : IGetPlotWeatherForecastQueryService
{
    public async Task<Result<PlotWeatherForecastResource, Error>> Handle(GetPlotWeatherForecastQuery query, CancellationToken cancellationToken = default)
    {
        var plot = await plotRepository.FindByIdAsync(query.PlotId, cancellationToken);
        if (plot is null || plot.IsDeleted)
            return new Result<PlotWeatherForecastResource, Error>.Failure(new Error("PLOT_NOT_FOUND", "Plot not found."));

        var hourly = new List<HourlyForecastResource>
        {
            new HourlyForecastResource(DateTimeOffset.UtcNow, 22.5, "Sunny", 45, 0.0, 3.5, 5.0)
        };

        var daily = new List<DailyForecastResource>
        {
            new DailyForecastResource(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), 15.0, 28.0, 21.5, "Sunny", 50, 0.0, 6.0)
        };

        var warnings = new List<WeatherWarningResource>
        {
            new WeatherWarningResource("Frost", "Low", DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), "No frost expected")
        };

        var source = new ExternalSourceResource("OpenWeather", "Online", DateTimeOffset.UtcNow, 180);

        var resource = new PlotWeatherForecastResource(
            plot.Id,
            plot.OwnerUserId,
            plot.PlotName,
            DateTimeOffset.UtcNow,
            hourly,
            daily,
            0.5,
            "Low",
            warnings,
            source
        );

        return new Result<PlotWeatherForecastResource, Error>.Success(resource);
    }
}
