using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.OutboundServices.Acl;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using Cortex.Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.CommandServices;

public class PestSightingCommandService(
    IPestSightingReportRepository pestSightingReportRepository,
    IExternalAgronomicService externalAgronomicService,
    ThreatInferenceService threatInferenceService,
    IAlertCommandService alertCommandService,
    IUnitOfWork unitOfWork,
    IMediator mediator)
    : IPestSightingCommandService
{
    public async Task<Result<PestSightingReport, Error>> Handle(CreatePestSightingReportCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var aggregate = new PestSightingReport(command);

            var currentNdvi = await externalAgronomicService.FetchCurrentNdviByPlotIdAsync(command.PlotId, command.ReporterUserId, cancellationToken);
            
            var inferredThreat = threatInferenceService.InferFromSymptoms(aggregate.Symptoms);

            aggregate.EvaluateBiologicalRisk(currentNdvi, inferredThreat);

            await pestSightingReportRepository.AddAsync(aggregate, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            var domainEvent = new PestSightingReportEvaluatedEvent(
                aggregate.Id,
                aggregate.PlotId.Value,
                aggregate.ReporterUserId.Value,
                aggregate.CalculatedRisk.ToString(),
                aggregate.ProbableThreat.ToString(),
                aggregate.AlertConfirmed,
                aggregate.Status.ToString()
            );

            await mediator.PublishAsync(domainEvent, cancellationToken);

            return new Result<PestSightingReport, Error>.Success(aggregate);
        }
        catch (OperationCanceledException)
        {
            return new Result<PestSightingReport, Error>.Failure(SurveillanceErrors.OperationCancelled);
        }
        catch (DbUpdateException ex)
        {
            return new Result<PestSightingReport, Error>.Failure(SurveillanceErrors.DatabaseError);
        }
        catch (Exception ex)
        {
            return new Result<PestSightingReport, Error>.Failure(SurveillanceErrors.InternalServerError);
        }
    }

    public async Task<Result<PestSightingReport, Error>> Handle(ReviewPestSightingReportCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Find report by id
            var report = await pestSightingReportRepository.FindByIdAsync((int)command.ReportId, cancellationToken);
            if (report is null)
            {
                return new Result<PestSightingReport, Error>.Failure(SurveillanceErrors.NotFound);
            }

            // 2. Validate ownership
            if (report.ReporterUserId.Value != command.ReporterUserId)
            {
                return new Result<PestSightingReport, Error>.Failure(
                    new Error("Surveillance.OwnershipMismatch", "Report does not belong to the requesting user."));
            }

            // 3. Parse outcome
            if (!Enum.TryParse<EReportStatus>(command.Outcome, true, out var outcome)
                || outcome is not (EReportStatus.CONFIRMED or EReportStatus.RULED_OUT))
            {
                return new Result<PestSightingReport, Error>.Failure(
                    new Error("Surveillance.InvalidOutcome", "Outcome must be CONFIRMED or RULED_OUT."));
            }

            // 4. Call aggregate method
            try
            {
                switch (outcome)
                {
                    case EReportStatus.CONFIRMED:
                        report.ConfirmAfterInspection();
                        break;
                    case EReportStatus.RULED_OUT:
                        report.DismissAfterInspection();
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                return new Result<PestSightingReport, Error>.Failure(
                    new Error("Surveillance.InvalidStateTransition", ex.Message));
            }

            // 5. Save the entity
            pestSightingReportRepository.Update(report);
            await unitOfWork.CompleteAsync(cancellationToken);

            // 6. Mirror onto alert
            if (report.Status == EReportStatus.CONFIRMED)
            {
                var confirmResult = await alertCommandService.Handle(
                    new ConfirmAlertFromInspectionCommand(report.Id), cancellationToken);

                bool noLinkedAlert = confirmResult.IsSuccess && ((Result<long, Error>.Success)confirmResult).Value == 0L;
                if (noLinkedAlert)
                {
                    var domainEvent = new PestSightingReportEvaluatedEvent(
                        report.Id,
                        report.PlotId.Value,
                        report.ReporterUserId.Value,
                        report.CalculatedRisk.ToString(),
                        report.ProbableThreat.ToString(),
                        report.AlertConfirmed,
                        report.Status.ToString());
                    await mediator.PublishAsync(domainEvent, cancellationToken);
                }
            }
            else if (report.Status == EReportStatus.RULED_OUT)
            {
                await alertCommandService.Handle(
                    new DismissReportAlertCommand(
                        report.Id,
                        "The grower ruled this out as a verified false positive after a field inspection."),
                    cancellationToken);
            }

            return new Result<PestSightingReport, Error>.Success(report);
        }
        catch (OperationCanceledException)
        {
            return new Result<PestSightingReport, Error>.Failure(SurveillanceErrors.OperationCancelled);
        }
        catch (Exception ex)
        {
            return new Result<PestSightingReport, Error>.Failure(SurveillanceErrors.InternalServerError);
        }
    }
}
