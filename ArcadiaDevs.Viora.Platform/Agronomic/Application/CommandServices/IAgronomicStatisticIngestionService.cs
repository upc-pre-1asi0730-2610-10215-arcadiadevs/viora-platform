using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;

public interface IAgronomicStatisticIngestionService
{
    Task<Result<AgronomicStatisticsIngestionReport, Error>> Handle(
        IngestAgronomicStatisticsCommand command,
        CancellationToken cancellationToken = default);

    Task<AgronomicStatisticsIngestionReport> IngestAllActivePlotsAsync(CancellationToken cancellationToken = default);

    Task<bool> IngestForPlotAsync(Plot plot, CancellationToken cancellationToken = default);
}
