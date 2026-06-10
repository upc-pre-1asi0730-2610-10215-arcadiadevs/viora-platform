using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
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
        // Verify that the referenced plot exists (repository expects int id)
        var plot = await _plotRepository.FindByIdAsync(command.PlotId);
        if (plot == null || plot.IsDeleted)
        {
            return new Result<IoTDevice, Error>.Failure(
                new Error("PLOT_NOT_FOUND", "Plot " + command.PlotId.ToString() + " not found"));
        }

        // Create domain entity using domain value object for PlotId
        var device = new IoTDevice(
            new PlotId(command.PlotId),
            command.DeviceName,
            command.Status);

        var saved = await _ioTDeviceRepository.SaveAsync(device);
        return new Result<IoTDevice, Error>.Success(saved);
    }

    public async Task<Result<IoTDevice, Error>> Handle(
        UpdateIoTDeviceCommand command, 
        CancellationToken cancellationToken = default)
    {
        // Update is not implemented in this service (ignored per request)
        return new Result<IoTDevice, Error>.Failure(
            new Error("NOT_IMPLEMENTED", "Update operation is not implemented."));
    }

    public async Task<Result<bool, Error>> Handle(
        DeleteIoTDeviceCommand command, 
        CancellationToken cancellationToken = default)
    {
        // Delete is not implemented in this service (ignored per request)
        return new Result<bool, Error>.Failure(
            new Error("NOT_IMPLEMENTED", "Delete operation is not implemented."));
    }
}