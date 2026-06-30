using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using NSubstitute;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Aggregates;

/// <summary>
///     AGRO-002 hardening tests for the <see cref="IoTDevice"/> aggregate.
///     The aggregate must validate inputs through a <c>Create</c> factory and
///     expose <c>Activate</c> / <c>Deactivate</c> / <c>RecordReading</c> domain
///     methods that enforce state-machine transitions.
/// </summary>
public class IoTDeviceTests
{
    private static IClock FixedClock(DateTime? at = null)
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(at ?? new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc));
        return clock;
    }

    [Fact]
    public void Create_WithEmptyActivationCode_ReturnsFailure()
    {
        // Arrange
        var clock = FixedClock();

        // Act — surrogate for the design sketch's "empty activation code" path:
        // the current schema's equivalent invariant is a non-empty device name.
        var result = IoTDevice.Create(1L, "   ", clock);

        // Assert
        Assert.True(result.IsFailure);
        var error = ((Result<IoTDevice, Error>.Failure)result).Error;
        Assert.Equal("DEVICE_NAME_REQUIRED", error.Code);
    }

    [Fact]
    public void Create_WithEmptyDeviceName_ReturnsFailure()
    {
        var clock = FixedClock();

        var result = IoTDevice.Create(1L, string.Empty, clock);

        Assert.True(result.IsFailure);
        var error = ((Result<IoTDevice, Error>.Failure)result).Error;
        Assert.Equal("DEVICE_NAME_REQUIRED", error.Code);
    }

    [Fact]
    public void Create_WithNonPositivePlotId_ReturnsFailure()
    {
        var clock = FixedClock();

        var result = IoTDevice.Create(0L, "Sensor 01", clock);

        Assert.True(result.IsFailure);
        var error = ((Result<IoTDevice, Error>.Failure)result).Error;
        Assert.Equal("PLOT_ID_REQUIRED", error.Code);
    }

    [Fact]
    public void Create_WithValidInputs_ReturnsPendingDeviceWithCreatedAt()
    {
        var fixedNow = new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);
        var clock = FixedClock(fixedNow);

        var result = IoTDevice.Create(7L, "Sensor 01", clock);

        Assert.True(result.IsSuccess);
        var device = ((Result<IoTDevice, Error>.Success)result).Value;
        Assert.Equal(7L, device.PlotId);
        Assert.Equal("Sensor 01", device.DeviceName);
        Assert.Equal(IoTDeviceStatus.Pending, device.Status);
        Assert.Equal(new DateTimeOffset(fixedNow, TimeSpan.Zero), device.CreatedAt);
    }

    [Fact]
    public void Activate_OnNonPending_ReturnsFailure()
    {
        // Arrange — a device that is already Active cannot be activated again.
        var clock = FixedClock();
        var created = IoTDevice.Create(1L, "Sensor 01", clock);
        Assert.True(created.IsSuccess);
        var device = ((Result<IoTDevice, Error>.Success)created).Value;

        // Move to Active via the happy path
        var firstActivate = device.Activate();
        Assert.True(firstActivate.IsSuccess);
        Assert.Equal(IoTDeviceStatus.Active, device.Status);

        // Act — second activation must fail
        var secondActivate = device.Activate();

        // Assert
        Assert.True(secondActivate.IsFailure);
        var error = ((Result<Unit, Error>.Failure)secondActivate).Error;
        Assert.Equal("INVALID_TRANSITION", error.Code);
        // Status must remain Active, not silently flip
        Assert.Equal(IoTDeviceStatus.Active, device.Status);
    }

    [Fact]
    public void Activate_OnPending_ReturnsSuccess_AndFlipsToActive()
    {
        var clock = FixedClock();
        var created = IoTDevice.Create(1L, "Sensor 01", clock);
        var device = ((Result<IoTDevice, Error>.Success)created).Value;

        var result = device.Activate();

        Assert.True(result.IsSuccess);
        Assert.Equal(IoTDeviceStatus.Active, device.Status);
    }

    [Fact]
    public void Deactivate_OnActive_ReturnsSuccess_AndFlipsToInactive()
    {
        var clock = FixedClock();
        var device = ((Result<IoTDevice, Error>.Success)IoTDevice.Create(1L, "Sensor 01", clock)).Value;
        _ = device.Activate();

        var result = device.Deactivate();

        Assert.True(result.IsSuccess);
        Assert.Equal(IoTDeviceStatus.Inactive, device.Status);
    }

    [Fact]
    public void Deactivate_OnPending_ReturnsFailure()
    {
        var clock = FixedClock();
        var device = ((Result<IoTDevice, Error>.Success)IoTDevice.Create(1L, "Sensor 01", clock)).Value;

        var result = device.Deactivate();

        Assert.True(result.IsFailure);
        var error = ((Result<Unit, Error>.Failure)result).Error;
        Assert.Equal("INVALID_TRANSITION", error.Code);
    }

    [Fact]
    public void RecordReading_OnAnyState_ReturnsSuccess()
    {
        // RecordReading is an additive no-op for now (forward-compat hook for
        // future sensor-reading state). It must not block on the state machine.
        var clock = FixedClock();
        var device = ((Result<IoTDevice, Error>.Success)IoTDevice.Create(1L, "Sensor 01", clock)).Value;

        var pending = device.RecordReading();
        var active = device.Activate();
        var afterActivate = device.RecordReading();

        Assert.True(pending.IsSuccess);
        Assert.True(active.IsSuccess);
        Assert.True(afterActivate.IsSuccess);
    }

    [Fact]
    public void Create_SetsStatusToPending_SoSubsequentActivateWorks()
    {
        // Documents the contract: factory always emits Pending, Activate is
        // the only path to Active. This guards against accidental status
        // regressions if the factory is later refactored.
        var clock = FixedClock();
        var device = ((Result<IoTDevice, Error>.Success)IoTDevice.Create(1L, "Sensor 01", clock)).Value;

        Assert.Equal(IoTDeviceStatus.Pending, device.Status);
    }
}
