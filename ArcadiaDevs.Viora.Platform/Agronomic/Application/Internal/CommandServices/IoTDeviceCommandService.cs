using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

public class IoTDeviceCommandService : IIoTDeviceCommandService
{
    private readonly IIoTDeviceRepository _ioTDeviceRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly IClock _clock;

    public IoTDeviceCommandService(
        IIoTDeviceRepository ioTDeviceRepository,
        IPlotRepository plotRepository,
        IClock clock)
    {
        _ioTDeviceRepository = ioTDeviceRepository;
        _plotRepository = plotRepository;
        _clock = clock;
    }

    public async Task<Result<IoTDevice, Error>> Handle(
        CreateIoTDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var plot = await _plotRepository.FindByIdAsync(command.PlotId);
        if (plot == null || plot.IsDeleted)
        {
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.PlotNotFound);
        }

        // AGRO-002: route through the factory so private setters + state
        // machine are the only way to instantiate an IoTDevice.
        var createResult = IoTDevice.Create(
            plotId: command.PlotId,
            deviceName: command.DeviceName,
            clock: _clock);
        if (createResult is Result<IoTDevice, Error>.Failure failure)
        {
            return failure;
        }

        var device = ((Result<IoTDevice, Error>.Success)createResult).Value;
        var saved = await _ioTDeviceRepository.SaveAsync(device);
        return new Result<IoTDevice, Error>.Success(saved);
    }

    public async Task<Result<IoTDevice, Error>> Handle(
        UpdateIoTDeviceCommand command,
        CancellationToken cancellationToken = default)
    {

        var plot = await _plotRepository.FindByIdAsync(command.PlotId);
        if (plot == null || plot.IsDeleted)
        {
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.PlotNotFound);
        }

        var device = await _ioTDeviceRepository.FindByIdAndPlotIdAsync(command.DeviceId, command.PlotId);
        if (device == null)
        {
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.DeviceNotFound);
        }

        var updateResult = device.UpdateInformation(new DeviceName(command.DeviceName), command.Status);
        if (updateResult is Result<Unit, Error>.Failure updateFailure)
        {
            return new Result<IoTDevice, Error>.Failure(updateFailure.Error);
        }

        var saved = await _ioTDeviceRepository.SaveAsync(device);

        return new Result<IoTDevice, Error>.Success(saved);
    }

    public async Task<Result<bool, Error>> Handle(
        DeleteIoTDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var plot = await _plotRepository.FindByIdAsync(command.PlotId);
        if (plot == null || plot.IsDeleted)
        {
            return new Result<bool, Error>.Failure(
                AgronomicErrors.PlotNotFound);
        }

        var device = await _ioTDeviceRepository.FindByIdAndPlotIdAsync(command.DeviceId, command.PlotId);
        if (device == null)
        {
            return new Result<bool, Error>.Failure(
                AgronomicErrors.DeviceNotFound);
        }

        await _ioTDeviceRepository.DeleteAsync(device);

        return new Result<bool, Error>.Success(true);
    }
}