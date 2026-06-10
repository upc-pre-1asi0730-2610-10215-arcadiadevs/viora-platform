using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

public record UpdateIoTDeviceCommand(long DeviceId, int PlotId, string DeviceName, IoTDeviceStatus? Status);


