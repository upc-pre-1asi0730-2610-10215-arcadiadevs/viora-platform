using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record IoTDeviceResource(
    int Id,
    int PlotId,
    string DeviceName,
    IoTDeviceStatus Status,
    string? Health,
    string? DeviceType,
    int? SoilMoisture,
    double? Temperature,
    int? LeafHumidity,
    string? LastUpdate);
