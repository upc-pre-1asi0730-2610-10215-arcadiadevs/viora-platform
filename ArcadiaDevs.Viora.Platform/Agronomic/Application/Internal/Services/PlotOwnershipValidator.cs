using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;

public class PlotOwnershipValidator(IPlotRepository plotRepository)
{
    public async Task<Result<Plot, Error>> ValidateAsync(int userId, int plotId, CancellationToken ct = default)
    {
        var plot = await plotRepository.FindByIdAsync(plotId, ct);
        if (plot is null || !plot.IsActive)
        {
            return new Result<Plot, Error>.Failure(new Error("Agronomic.PlotNotFound", "The specified plot was not found or is inactive."));
        }
        if (!plot.BelongsTo(userId))
        {
            return new Result<Plot, Error>.Failure(new Error("Agronomic.Forbidden", $"User {userId} does not own plot {plotId}."));
        }
        return new Result<Plot, Error>.Success(plot);
    }
}
