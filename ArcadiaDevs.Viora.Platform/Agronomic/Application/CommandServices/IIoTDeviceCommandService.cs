using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;

public interface IIoTDeviceCommandService
{
    Task<Result<IoTDevice, Error>> Handle(CreateIoTDeviceCommand command, CancellationToken cancellationToken = default);
    Task<Result<IoTDevice, Error>> Handle(
        UpdateIoTDeviceCommand command, 
        CancellationToken cancellationToken = default);

    Task<Result<bool, Error>> Handle(
        DeleteIoTDeviceCommand command, 
        CancellationToken cancellationToken = default);
}