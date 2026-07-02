using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Adapters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Hosting;

/// <summary>
/// Extension methods for registering agronomic statistics services.
/// </summary>
public static class AgronomicStatisticsHostingExtensions
{
    /// <summary>
    /// Registers the agronomic statistics services including options binding,
    /// domain services, and the scheduled ingestion background service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgronomicStatisticsHosting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind options from configuration
        services.AddOptionsWithValidateOnStart<AgronomicStatisticsOptions>()
            .Bind(configuration.GetSection(AgronomicStatisticsOptions.SectionName));

        // Register domain services as singletons (pure functions, no I/O)
        services.AddSingleton<NdviTrendAnalyzer>();
        services.AddSingleton<PlotHealthEvaluator>();
        services.AddSingleton<IMitigationRecommendationGenerator, MitigationRecommendationGenerator>();
        services.AddSingleton<IWeatherForecastAdvisor, WeatherForecastAdvisor>();

        // Register the scheduled ingestion background service
        services.AddHostedService<AgronomicStatisticIngestionScheduler>();

        // Register 1.17.1 services
        services.AddScoped<IPlotDetailMetadataProvider, JpaPlotDetailMetadataProvider>();
        services.AddScoped<PlotOwnershipValidator>();

        return services;
    }
}