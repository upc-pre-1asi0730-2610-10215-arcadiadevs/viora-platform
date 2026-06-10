using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

public record DeleteIoTDeviceCommand(long DeviceId, int PlotId);

