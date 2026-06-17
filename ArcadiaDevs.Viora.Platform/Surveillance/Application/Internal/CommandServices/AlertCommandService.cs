using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using Cortex.Mediator;
using Microsoft.EntityFrameworkCore;

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

            return new Result<Alert, Error>.Success(alert);
        }
        catch (OperationCanceledException)
        {
            return new Result<Alert, Error>.Failure(new Error("OperationCancelled", "The operation was cancelled."));
        }

        catch (DbUpdateException ex)
        {
            return new Result<Alert, Error>.Failure(new Error("DatabaseError", $"A database error occurred: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return new Result<Alert, Error>.Failure(new Error("InternalServerError", $"An unexpected error occurred: {ex.Message}"));
        }
    }

    public async Task<Result<long, Error>> Handle(MarkAlertAsReviewedCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await alertRepository.FindByIdAsync((int)command.AlertId, cancellationToken);
            if (alert is null)
            {
                return new Result<long, Error>.Failure(new Error("NotFound", $"Alert with id {command.AlertId} not found"));
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
            return new Result<long, Error>.Failure(new Error("InternalServerError", $"Failed to mark alert as reviewed: {ex.Message}"));
        }
    }
}
