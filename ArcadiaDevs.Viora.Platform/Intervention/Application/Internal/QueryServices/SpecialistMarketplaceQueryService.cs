using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Acl;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Assembles the specialist Intervention Marketplace read model. The
///     incoming inbox is the specialist's PENDING requests. Each case is
///     enriched across contexts through ACL facades: severity and problem
///     from the linked Surveillance alert, plot name/location/area/crop and
///     current NDVI from Agronomic, and the producer's display name/photo
///     from Profile. Mirrors OS's <c>SpecialistMarketplaceQueryServiceImpl</c>.
/// </summary>
/// <remarks>
///     No fabricated data: a field with no source for a given case is left
///     <c>null</c> (the client renders an empty state), and case distance is
///     omitted entirely because the specialist has no real geolocation
///     wired into this card. The acceptance rate is <c>null</c> until the
///     specialist has decided proposals, matching the dashboard.
/// </remarks>
public class SpecialistMarketplaceQueryService(
    IInterventionRequestRepository interventionRequestRepository,
    IServiceProposalRepository serviceProposalRepository,
    ISurveillanceContextFacade surveillanceContextFacade,
    IAgronomicContextFacade agronomicContextFacade,
    IProfileContextFacade profileContextFacade,
    IClock clock)
    : ISpecialistMarketplaceQueryService
{
    public async Task<SpecialistMarketplaceResource> Handle(
        GetSpecialistMarketplaceQuery query,
        CancellationToken cancellationToken = default)
    {
        var pending = await interventionRequestRepository.FindBySpecialistIdAndStatusAsync(
            query.SpecialistId, InterventionStatus.PENDING, cancellationToken);
        var accepted = await interventionRequestRepository.FindBySpecialistIdAndStatusAsync(
            query.SpecialistId, InterventionStatus.ACCEPTED, cancellationToken);
        var proposals = await serviceProposalRepository.FindBySpecialistIdAsync(
            query.SpecialistId, cancellationToken);

        var cases = new List<SpecialistMarketplaceResource.MarketplaceCase>(pending.Count);
        foreach (var request in pending)
        {
            cases.Add(await ToCaseAsync(request, cancellationToken));
        }

        return new SpecialistMarketplaceResource(
            cases.Count,
            AcceptanceRatePercent(proposals),
            accepted.Count,
            cases,
            new DateTimeOffset(clock.UtcNow, TimeSpan.Zero).ToString("O"));
    }

    private async Task<SpecialistMarketplaceResource.MarketplaceCase> ToCaseAsync(
        InterventionRequest request,
        CancellationToken cancellationToken)
    {
        var alert = request.AlertId is { } alertId
            ? await surveillanceContextFacade.GetAlertCardSummaryAsync(alertId, cancellationToken)
            : null;
        var plot = await agronomicContextFacade.GetPlotCardSummaryAsync(request.PlotId, cancellationToken);
        var ndvi = await agronomicContextFacade.FetchCurrentNdviByPlotAsync(request.PlotId, cancellationToken);
        var producerName = await profileContextFacade.GetDisplayNameAsync(request.GrowerId, cancellationToken)
            ?? $"Grower #{request.GrowerId}";
        var producerPhotoUrl = await profileContextFacade.GetPhotoUrlAsync(request.GrowerId, cancellationToken);
        var plotCount = await agronomicContextFacade.CountPlotsByUserAsync(request.GrowerId, cancellationToken);

        return new SpecialistMarketplaceResource.MarketplaceCase(
            request.Id,
            $"REQ-{request.Id}",
            request.SpecialistId,
            alert?.Severity,
            ProblemLabel(alert, request),
            ndvi,
            producerName,
            producerPhotoUrl,
            BlankToNull(plot?.CropType),
            plot?.Name ?? $"Plot #{request.PlotId}",
            BlankToNull(plot?.Location),
            plot?.AreaHectares,
            plotCount,
            request.CreatedAt?.ToString("O"));
    }

    /// <summary>Prefers the alert's problem label; falls back to the request's stated reason.</summary>
    private static string? ProblemLabel(AlertCardSummary? alert, InterventionRequest request)
    {
        if (alert is not null && !string.IsNullOrWhiteSpace(alert.ProblemLabel))
        {
            return alert.ProblemLabel;
        }

        return string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason;
    }

    /// <summary>
    ///     Acceptance rate as accepted vs. decided proposals, or <c>null</c>
    ///     when the specialist has no decided proposals yet (so the client
    ///     shows an empty state rather than a misleading 0%).
    /// </summary>
    private static double? AcceptanceRatePercent(IReadOnlyList<ServiceProposal> proposals)
    {
        var acceptedProposals = proposals.Count(p => p.Status == ServiceProposalStatus.ACCEPTED);
        var decidedProposals = proposals.Count(p =>
            p.Status == ServiceProposalStatus.ACCEPTED || p.Status == ServiceProposalStatus.REJECTED);

        if (decidedProposals == 0)
        {
            return null;
        }

        return Math.Round(acceptedProposals * 100.0 / decidedProposals, 1);
    }

    private static string? BlankToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
