using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;

/// <summary>
///     Service for handling GetPlotNdviTileQuery.
/// </summary>
public interface IGetPlotNdviTileQueryService
{
    Task<Result<byte[], Error>> HandleAsync(GetPlotNdviTileQuery query, CancellationToken cancellationToken = default);
}
