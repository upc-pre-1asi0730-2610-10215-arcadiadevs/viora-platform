using System.Threading;
using System.Threading.Tasks;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;

/// <summary>
///     Produces <see cref="Domain.Model.Events.NdviDroppedIntegrationEvent"/>
///     for active plots whose latest NDVI reading has dropped significantly
///     below the plot's own historical average. Wired into the daily
///     <see cref="Infrastructure.Hosting.AgronomicStatisticIngestionScheduler"/>
///     via <see cref="Microsoft.Extensions.DependencyInjection.IServiceScopeFactory"/>.
/// </summary>
public interface INdviDroppedIntegrationEventProducer
{
    Task ProduceNdviDroppedEventsAsync(CancellationToken ct = default);
}
