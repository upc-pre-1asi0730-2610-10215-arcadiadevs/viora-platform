using System;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;

public class IoTDeviceCommandService : IIoTDeviceCommandService
{
    private readonly IIoTDeviceRepository _ioTDeviceRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly IActivationCodeCatalog _catalog;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public IoTDeviceCommandService(
        IIoTDeviceRepository ioTDeviceRepository,
        IPlotRepository plotRepository,
        IActivationCodeCatalog catalog,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _ioTDeviceRepository = ioTDeviceRepository;
        _plotRepository = plotRepository;
        _catalog = catalog;
        _unitOfWork = unitOfWork;
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

        if (!plot.BelongsTo(command.UserId))
        {
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.UnauthorizedAccess);
        }

        // A4 part 2: parse the activation code into the VO first. The VO's
        // constructor throws ArgumentException on null/blank/malformed input;
        // we surface that as a domain failure instead of letting it bubble up.
        ActivationCode code;
        try
        {
            code = new ActivationCode(command.ActivationCode);
        }
        catch (ArgumentException)
        {
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.InvalidActivationCodeFormat);
        }

        // Catalog check: the format may be valid but the code must correspond
        // to a real issued sensor unit. A miss means a typo or a counterfeit.
        if (!_catalog.IsIssued(code))
        {
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.ActivationCodeNotRecognized);
        }

        // Uniqueness check: a code can only be claimed once. The pre-flight
        // query is the primary guard; the unique index on iot_devices.activation_code
        // + the DbUpdateException catch below are the backstop for the rare
        // double-claim race.
        if (await _ioTDeviceRepository.ExistsByActivationCodeAsync(code, cancellationToken))
        {
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.ActivationCodeAlreadyClaimed);
        }

        // Route through the Claim factory so the private setters + state machine
        // are the only way to instantiate an IoTDevice (AGRO-002 + A4 part 2).
        var claimResult = IoTDevice.Claim(
            plotId: command.PlotId,
            deviceName: command.DeviceName,
            code: code,
            clock: _clock);
        if (claimResult is Result<IoTDevice, Error>.Failure claimFailure)
        {
            return claimFailure;
        }

        var device = ((Result<IoTDevice, Error>.Success)claimResult).Value;

        try
        {
            await _ioTDeviceRepository.AddAsync(device, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsActivationCodeUniqueViolation(ex))
        {
            // A concurrent claim slipped past the pre-flight check; map the
            // database-level uniqueness violation to the same domain failure
            // the pre-flight check produces. Postgres SQLSTATE 23505.
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.ActivationCodeAlreadyClaimed);
        }

        return new Result<IoTDevice, Error>.Success(device);
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

        if (!plot.BelongsTo(command.UserId))
        {
            return new Result<IoTDevice, Error>.Failure(
                AgronomicErrors.UnauthorizedAccess);
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

        if (!plot.BelongsTo(command.UserId))
        {
            return new Result<bool, Error>.Failure(
                AgronomicErrors.UnauthorizedAccess);
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

    /// <summary>
    ///     Detects whether a <see cref="DbUpdateException"/> was caused by a
    ///     unique-index violation on the <c>iot_devices.activation_code</c>
    ///     index. Only Postgres 23505 wrapping an
    ///     <see cref="Postgres.Npgsql.PostgresException"/> with
    ///     <c>ConstraintName == ix_iot_devices_activation_code</c> is treated
    ///     as the activation-code race; any other constraint violation is
    ///     rethrown so callers can see the real cause.
    /// </summary>
    private static bool IsActivationCodeUniqueViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
        {
            var typeName = inner.GetType().FullName ?? string.Empty;
            if (typeName == "Npgsql.PostgresException")
            {
                var sqlState = inner.GetType().GetProperty("SqlState")?.GetValue(inner) as string;
                var constraintName = inner.GetType().GetProperty("ConstraintName")?.GetValue(inner) as string;
                // SQLSTATE 23505 = unique_violation. The ConstraintName check
                // is belt-and-suspenders: any other 23505 (e.g., an unrelated
                // future index) would not match ix_iot_devices_activation_code
                // and would fall through to a rethrow.
                return sqlState == "23505"
                    && constraintName == "ix_iot_devices_activation_code";
            }
        }

        return false;
    }
}
