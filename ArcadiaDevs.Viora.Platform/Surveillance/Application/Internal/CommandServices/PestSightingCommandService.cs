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
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using Cortex.Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Application.Internal.CommandServices;

public class PestSightingCommandService(
    IPestSightingReportRepository pestSightingReportRepository,
    IExternalAgronomicService externalAgronomicService,
    ThreatInferenceService threatInferenceService,
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
}
