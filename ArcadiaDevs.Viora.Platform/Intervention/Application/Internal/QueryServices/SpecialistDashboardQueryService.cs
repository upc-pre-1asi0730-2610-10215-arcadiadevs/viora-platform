using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Domain;

namespace ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Assembles the specialist dashboard read model. Real, repository-derived
///     data only: resolved interventions (accepted requests assigned to the
///     specialist), acceptance rate (accepted vs. decided service proposals),
///     the incoming request inbox (pending requests), and the accepted-cases
///     performance series (grouped from the specialist's proposals). Mirrors
///     OS's <c>SpecialistDashboardQueryServiceImpl.java</c>.
/// </summary>
/// <remarks>
///     No fabricated data: metrics with no domain source yet are returned
///     empty — the acceptance rate delta is null (needs a historical
///     snapshot not persisted yet), the phytosanitary efficiency/status are
///     null (no intervention-outcome read model yet), and the zonal radar is
///     empty (no geospatial surveillance projection yet).
/// </remarks>
public class SpecialistDashboardQueryService(
    IInterventionRequestRepository interventionRequestRepository,
    IServiceProposalRepository serviceProposalRepository,
    IClock clock) : ISpecialistDashboardQueryService
{
    private static readonly string[] MonthLabels =
    {
        "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
        "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
    };

    public async Task<SpecialistDashboardResource> Handle(
        GetSpecialistDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var pending = await interventionRequestRepository.FindBySpecialistIdAndStatusAsync(
            query.SpecialistId, InterventionStatus.PENDING, cancellationToken);
        var accepted = await interventionRequestRepository.FindBySpecialistIdAndStatusAsync(
            query.SpecialistId, InterventionStatus.ACCEPTED, cancellationToken);
        var proposals = await serviceProposalRepository.FindBySpecialistIdAsync(
            query.SpecialistId, cancellationToken);

        var resolvedInterventions = accepted.Count;
        var acceptanceRate = AcceptanceRatePercent(proposals);

        var incoming = pending.Select(ToIncoming).ToList();
        var monthly = BuildMonthlySeries(proposals);
        var annual = BuildAnnualSeries(proposals);

        return new SpecialistDashboardResource(
            resolvedInterventions,
            acceptanceRate,
            null, // Delta needs a historical snapshot we do not persist yet.
            null, // Phytosanitary efficiency: no intervention-outcome source yet.
            null,
            Array.Empty<SpecialistDashboardResource.ZonalRisk>(), // No geospatial surveillance source yet.
            incoming,
            monthly,
            annual,
            new DateTimeOffset(clock.UtcNow, TimeSpan.Zero).ToString("O"));
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

    private static SpecialistDashboardResource.IncomingRequest ToIncoming(InterventionRequest request)
    {
        return new SpecialistDashboardResource.IncomingRequest(
            request.Id,
            $"REQ-{request.Id}",
            $"Plot #{request.PlotId}",
            $"Grower #{request.GrowerId}",
            request.Reason,
            request.CreatedAt?.ToString("O"));
    }

    private static List<SpecialistDashboardResource.PerformancePoint> BuildMonthlySeries(
        IReadOnlyList<ServiceProposal> proposals)
    {
        var year = DateTime.UtcNow.Year;
        var counts = new int[12];

        foreach (var proposal in proposals)
        {
            var date = AcceptedProposalDate(proposal);
            if (date is { } d && d.Year == year)
            {
                counts[d.Month - 1]++;
            }
        }

        var series = new List<SpecialistDashboardResource.PerformancePoint>(12);
        for (var month = 0; month < 12; month++)
        {
            series.Add(new SpecialistDashboardResource.PerformancePoint(MonthLabels[month], counts[month]));
        }

        return series;
    }

    private static List<SpecialistDashboardResource.PerformancePoint> BuildAnnualSeries(
        IReadOnlyList<ServiceProposal> proposals)
    {
        var currentYear = DateTime.UtcNow.Year;
        var series = new List<SpecialistDashboardResource.PerformancePoint>(4);

        for (var year = currentYear - 3; year <= currentYear; year++)
        {
            var count = proposals.Count(p => AcceptedProposalDate(p) is { } d && d.Year == year);
            series.Add(new SpecialistDashboardResource.PerformancePoint(year.ToString(), count));
        }

        return series;
    }

    private static DateOnly? AcceptedProposalDate(ServiceProposal proposal)
    {
        return proposal.Status == ServiceProposalStatus.ACCEPTED ? proposal.ProposedDate : null;
    }
}
