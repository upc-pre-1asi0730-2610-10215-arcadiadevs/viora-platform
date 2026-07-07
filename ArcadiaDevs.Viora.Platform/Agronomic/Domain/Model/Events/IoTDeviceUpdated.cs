using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;

/// <summary>
///     Domain event raised by the <see cref="Aggregates.IoTDevice"/> aggregate
///     when <see cref="Aggregates.IoTDevice.UpdateInformation"/> changes its
///     status (parity with OS's <c>IoTDeviceUpdated</c>). Raised via the
///     <see cref="IHasDomainEvents"/> collection and dispatched post-commit,
///     since the device is always an already-persisted, already-Id'd
///     aggregate at update time (unlike Plot's create-time event).
/// </summary>
public record IoTDeviceUpdated(
    long DeviceId,
    long PlotId,
    IoTDeviceStatus OldStatus,
    IoTDeviceStatus NewStatus
) : IEvent;
