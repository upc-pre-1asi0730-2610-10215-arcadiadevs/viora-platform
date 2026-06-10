using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

/// <summary>
///     IoTDevice aggregate root.
/// </summary>
public class IoTDevice
{
    // CORREGIDO: Cambiado a long para evitar el error "Cannot convert source type long to target type int"
    public long Id { get; set; }
    public long PlotId { get; set; }
    public string DeviceName { get; set; }
    public IoTDeviceStatus Status { get; set; }

    // Constructor vacío requerido por Entity Framework
    public IoTDevice() { }

    /// <summary>
    ///     Creates a new IoTDevice from a PlotId, DeviceName and IoTDeviceStatus.
    /// </summary>
    public IoTDevice(PlotId plotId, DeviceName deviceName, IoTDeviceStatus? status)
    {
        if (plotId == default)
            throw new ArgumentException("IoTDevice requires a valid PlotId", nameof(plotId));

        if (status == null)
            throw new ArgumentException("IoTDevice requires a valid Status", nameof(status));
        
        this.PlotId = plotId.Value; 
        this.DeviceName = deviceName.Value;
        this.Status = status.Value;
    }

    /// <summary>
    ///     Updates the device name and status.
    /// </summary>
    public void update(DeviceName newName, IoTDeviceStatus? newStatus)
    {
        if (newStatus == null)
            throw new ArgumentException("Status cannot be null", nameof(newStatus));

        this.DeviceName = newName.Value;
        this.Status = newStatus.Value;
        
    }
}