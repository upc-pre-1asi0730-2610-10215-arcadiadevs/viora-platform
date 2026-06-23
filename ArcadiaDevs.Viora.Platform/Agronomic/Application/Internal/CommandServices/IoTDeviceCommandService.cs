using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

public class IoTDeviceCommandService : IIoTDeviceCommandService
{
    private readonly IIoTDeviceRepository _ioTDeviceRepository;
    private readonly IPlotRepository _plotRepository;

    public IoTDeviceCommandService(
        IIoTDeviceRepository ioTDeviceRepository,
        IPlotRepository plotRepository)
    {
        _ioTDeviceRepository = ioTDeviceRepository;
        _plotRepository = plotRepository;
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
        
        var device = new IoTDevice(
            new PlotId(command.PlotId),
            new DeviceName(command.DeviceName),
            command.Status);

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
        
        device.update(new DeviceName(command.DeviceName), command.Status);
        
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