using System.Threading;
using System.Threading.Tasks;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;

/// <summary>
///     Produces <see cref="Domain.Model.Events.HydricStressDetectedIntegrationEvent"/>
///     for active plots where an IoT device reports soil moisture below the critical
///     threshold (20%). Wired into the daily <see cref="Infrastructure.Hosting.AgronomicStatisticIngestionScheduler"/>
///     via <see cref="Microsoft.Extensions.DependencyInjection.IServiceScopeFactory"/>.
/// </summary>
public interface IHydricStressDetectedIntegrationEventProducer
{
    Task ProduceHydricStressEventsAsync(CancellationToken ct = default);
}
