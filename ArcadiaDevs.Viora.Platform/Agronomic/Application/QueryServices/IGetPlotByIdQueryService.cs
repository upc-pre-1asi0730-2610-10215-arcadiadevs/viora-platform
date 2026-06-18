using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;

public interface IGetPlotByIdQueryService
{
    Task<Result<PlotResource, Error>> Handle(GetPlotByIdQuery query, CancellationToken cancellationToken = default);
}
