namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;

public record GetIoTDevicesByPlotIdQuery
{
    public int PlotId { get; init; }
    public int AuthenticatedUserId { get; init; }

    public GetIoTDevicesByPlotIdQuery(int plotId, int authenticatedUserId)
    {
        if (plotId <= 0)
            throw new ArgumentException("GetIoTDevicesByPlotIdQuery requires a valid PlotId.", nameof(plotId));

        if (authenticatedUserId <= 0)
            throw new ArgumentException("GetIoTDevicesByPlotIdQuery requires a valid AuthenticatedUserId.", nameof(authenticatedUserId));

        PlotId = plotId;
        AuthenticatedUserId = authenticatedUserId;
    }
}