using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using Cortex.Mediator;
using Microsoft.EntityFrameworkCore;
using Unit = ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Unit;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.CommandServices;

public class AlertCommandService(
    IAlertRepository alertRepository,
    IUnitOfWork unitOfWork,
    IMediator mediator)
    : IAlertCommandService
{
    public async Task<Result<Alert, Error>> Handle(CreateAlertCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = new Alert(command);

            await alertRepository.AddAsync(alert, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            var domainEvent = new AlertCreatedEvent(
                alert.Id,
                alert.PlotId.Value,
                alert.Type.ToString(),
                alert.Severity.ToString()
            );

            await mediator.PublishAsync(domainEvent, cancellationToken);

            // SURV-002: publish the cross-BC AlertGeneratedIntegrationEvent
            // on the in-process bus when the new alert's ThreatType is
            // PHENOLOGICAL_RISK. The Agronomic BC subscribes via
            // AlertGeneratedIntegrationEventHandler and recommends a
            // DynamicNutritionPlan for the wrapped Agronomic.PlotId (CC-1).
            // The event is published post-commit (the alert is already
            // persisted) and a handler failure MUST NOT roll back the
            // originating transaction (CC-2: single in-process bus, no
            // retry, no DLQ; handler exceptions are logged by the bus).
            if (alert.Type == EThreatType.PHENOLOGICAL_RISK)
            {
                var integrationEvent = new AlertGeneratedIntegrationEvent(
                    alert.PlotId.Value,
                    alert.Id,
                    alert.Type.ToString(),
                    DateTime.UtcNow
                );

                await mediator.PublishAsync(integrationEvent, cancellationToken);
            }

            return new Result<Alert, Error>.Success(alert);
        }
        catch (OperationCanceledException)
        {
            return new Result<Alert, Error>.Failure(SurveillanceErrors.OperationCancelled);
        }

        catch (DbUpdateException ex)
        {
            return new Result<Alert, Error>.Failure(SurveillanceErrors.DatabaseError);
        }
        catch (Exception ex)
        {
            return new Result<Alert, Error>.Failure(SurveillanceErrors.InternalServerError);
        }
    }

    public async Task<Result<long, Error>> Handle(MarkAlertAsReviewedCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await alertRepository.FindByIdAsync((int)command.AlertId, cancellationToken);
            if (alert is null)
            {
                return new Result<long, Error>.Failure(SurveillanceErrors.NotFound);
            }

            alert.MarkAsReviewed();
            alertRepository.Update(alert);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<long, Error>.Success(alert.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new Result<long, Error>.Failure(SurveillanceErrors.InternalServerError);
        }
    }

    // ============================================================
    // SURV-003: state-machine transition handlers
    // ============================================================
    public async Task<Result<Unit, Error>> Handle(ConfirmAlertCommand command, CancellationToken cancellationToken = default)
        => await ApplyStateMachineAsync(command.AlertId, a => a.ConfirmFromInspection(), cancellationToken);

    public async Task<Result<Unit, Error>> Handle(DismissAlertCommand command, CancellationToken cancellationToken = default)
        => await ApplyStateMachineAsync(command.AlertId, a => a.Dismiss(), cancellationToken);

    public async Task<Result<Unit, Error>> Handle(EscalateAlertCommand command, CancellationToken cancellationToken = default)
        => await ApplyStateMachineAsync(command.AlertId, a => a.Escalate(), cancellationToken);

    public async Task<Result<Unit, Error>> Handle(LinkAlertReportCommand command, CancellationToken cancellationToken = default)
        => await ApplyStateMachineAsync(
            command.AlertId,
            a => a.LinkReport(new PestSightingReportId(command.ReportId)),
            cancellationToken);

    public async Task<Result<Unit, Error>> Handle(AddAlertTimelineRecordCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await alertRepository.FindByIdAsync((int)command.AlertId, cancellationToken);
            if (alert is null)
            {
                return new Result<Unit, Error>.Failure(SurveillanceErrors.NotFound);
            }

            alert.AddTimelineRecord(command.Tag, command.Title, command.Description);
            alertRepository.Update(alert);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<Unit, Error>.Success(Unit.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new Result<Unit, Error>.Failure(SurveillanceErrors.InternalServerError);
        }
    }

    /// <summary>
    ///     Loads the alert, applies the state-machine transition, and
    ///     persists the result. Returns <see cref="SurveillanceErrors.NotFound"/>
    ///     if the alert does not exist, or the result of the state-machine
    ///     call (which may be a <see cref="Result{TValue, TError}.Failure"/>
    ///     for invalid transitions, e.g. <c>ALERT_TERMINAL</c>).
    /// </summary>
    private async Task<Result<Unit, Error>> ApplyStateMachineAsync(
        long alertId,
        Func<Alert, Result<Unit, Error>> transition,
        CancellationToken cancellationToken)
    {
        try
        {
            var alert = await alertRepository.FindByIdAsync((int)alertId, cancellationToken);
            if (alert is null)
            {
                return new Result<Unit, Error>.Failure(SurveillanceErrors.NotFound);
            }

            var result = transition(alert);
            if (result.IsFailure)
            {
                // State unchanged — do not persist; surface the failure.
                return result;
            }

            alertRepository.Update(alert);
            await unitOfWork.CompleteAsync(cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new Result<Unit, Error>.Failure(SurveillanceErrors.InternalServerError);
        }
    }

    public async Task<Result<long, Error>> Handle(ConfirmAlertFromInspectionCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await alertRepository.FindByLinkedReportIdAsync(command.ReportId, cancellationToken);
            if (alert is null)
            {
                return new Result<long, Error>.Success(0L);
            }

            var result = alert.ConfirmFromInspection();
            if (result.IsFailure)
            {
                return new Result<long, Error>.Failure(((Result<Unit, Error>.Failure)result).Error);
            }

            alertRepository.Update(alert);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<long, Error>.Success(alert.Id);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new Result<long, Error>.Failure(SurveillanceErrors.InternalServerError);
        }
    }

    public async Task<Result<Unit, Error>> Handle(DismissReportAlertCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await alertRepository.FindByLinkedReportIdAsync(command.ReportId, cancellationToken);
            if (alert is null)
            {
                return new Result<Unit, Error>.Failure(SurveillanceErrors.NotFound);
            }

            var result = alert.Dismiss();
            if (result.IsFailure)
            {
                return result;
            }

            alert.AddTimelineRecord("INSPECTION", "Report dismissed", command.DismissalReason);
            alertRepository.Update(alert);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<Unit, Error>.Success(Unit.Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new Result<Unit, Error>.Failure(SurveillanceErrors.InternalServerError);
        }
    }
}
