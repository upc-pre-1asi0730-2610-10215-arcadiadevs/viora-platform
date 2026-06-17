namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.OutboundServices.Acl;

public interface IExternalAgronomicService
{
    Task<double?> FetchCurrentNdviByPlotIdAsync(long plotId, long reporterUserId, CancellationToken cancellationToken = default);
}
