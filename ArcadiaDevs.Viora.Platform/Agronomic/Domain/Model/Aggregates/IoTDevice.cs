using System;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;

/// <summary>
///     IoTDevice aggregate root (AGRO-002 hardened).
///     Use the <see cref="Create"/> factory to enforce invariants; mutate state
///     exclusively through the <c>Activate</c> / <c>Deactivate</c> /
///     <c>RecordReading</c> domain methods, which validate transitions and
///     return a <see cref="Result{TValue, TError}"/>.
/// </summary>
public partial class IoTDevice
{
    public long Id { get; private set; }
    public long PlotId { get; private set; }
    public string DeviceName { get; private set; } = string.Empty;
    public IoTDeviceStatus Status { get; private set; }

    // Parameterless constructor for EF Core materialization.
    private IoTDevice() { }

    /// <summary>
    ///     Creates a new <see cref="IoTDevice"/> in <see cref="IoTDeviceStatus.Pending"/>.
    /// </summary>
    /// <param name="plotId">The owning plot identifier; must be positive.</param>
    /// <param name="deviceName">The human-readable device name; must not be empty.</param>
    /// <param name="clock">Clock used to stamp <c>CreatedAt</c>.</param>
    /// <returns>
    ///     A <see cref="Result{TValue, TError}"/> wrapping the device on success
    ///     or an <see cref="Error"/> on validation failure.
    /// </returns>
    public static Result<IoTDevice, Error> Create(
        long plotId,
        string deviceName,
        IClock clock)
    {
        if (plotId <= 0)
        {
            return new Result<IoTDevice, Error>.Failure(
                new Error("PLOT_ID_REQUIRED", "IoTDevice requires a positive PlotId."));
        }

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return new Result<IoTDevice, Error>.Failure(
                new Error("DEVICE_NAME_REQUIRED", "DeviceName must not be empty."));
        }

        ArgumentNullException.ThrowIfNull(clock);

        var device = new IoTDevice
        {
            PlotId = plotId,
            DeviceName = deviceName.Trim(),
            Status = IoTDeviceStatus.Pending,
            CreatedAt = new DateTimeOffset(clock.UtcNow, TimeSpan.Zero)
        };
        return new Result<IoTDevice, Error>.Success(device);
    }

    /// <summary>
    ///     Transitions the device from <see cref="IoTDeviceStatus.Pending"/> to
    ///     <see cref="IoTDeviceStatus.Active"/>. Returns a failure result and
    ///     leaves state unchanged on any other source state.
    /// </summary>
    public Result<Unit, Error> Activate()
    {
        if (Status != IoTDeviceStatus.Pending)
        {
            return new Result<Unit, Error>.Failure(
                new Error("INVALID_TRANSITION", $"Cannot activate a device in status {Status}."));
        }

        Status = IoTDeviceStatus.Active;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Transitions the device from <see cref="IoTDeviceStatus.Active"/> to
    ///     <see cref="IoTDeviceStatus.Inactive"/>. Returns a failure result and
    ///     leaves state unchanged on any other source state.
    /// </summary>
    public Result<Unit, Error> Deactivate()
    {
        if (Status != IoTDeviceStatus.Active)
        {
            return new Result<Unit, Error>.Failure(
                new Error("INVALID_TRANSITION", $"Cannot deactivate a device in status {Status}."));
        }

        Status = IoTDeviceStatus.Inactive;
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Marks a sensor reading event for the device. State-machine-agnostic:
    ///     always succeeds; provided as a forward-compatibility hook for the
    ///     cross-BC <c>IHasDomainEvents</c> dispatcher (CC-4) and any future
    ///     telemetry side-effects.
    /// </summary>
    public Result<Unit, Error> RecordReading()
    {
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    /// <summary>
    ///     Updates the device's mutable fields. Validates inputs first; on
    ///     validation failure returns a <see cref="Result{TValue, TError}.Failure"/>
    ///     and leaves state unchanged.
    /// </summary>
    public Result<Unit, Error> UpdateInformation(DeviceName newName, IoTDeviceStatus? newStatus)
    {
        if (newName.Value != default && string.IsNullOrWhiteSpace(newName.Value))
        {
            return new Result<Unit, Error>.Failure(
                new Error("DEVICE_NAME_REQUIRED", "DeviceName must not be empty."));
        }

        if (newStatus == null)
        {
            return new Result<Unit, Error>.Failure(
                new Error("STATUS_REQUIRED", "Status is required."));
        }

        DeviceName = newName.Value;
        Status = newStatus.Value;
        return new Result<Unit, Error>.Success(Unit.Value);
    }
}
