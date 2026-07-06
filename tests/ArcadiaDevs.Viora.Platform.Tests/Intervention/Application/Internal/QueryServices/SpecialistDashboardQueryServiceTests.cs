using System.Reflection;
using ArcadiaDevs.Viora.Platform.Intervention.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Application.Internal.QueryServices;

/// <summary>
///     Unit tests for <see cref="SpecialistDashboardQueryService"/>.
///     Template B: query service with NSubstitute mocks.
///     Covers the real, repository-derived fields (resolved interventions,
///     acceptance rate, incoming inbox, monthly/annual performance series)
///     and the 4 by-design null/empty fields documented on
///     <see cref="SpecialistDashboardQueryService"/>'s class remarks:
///     <c>AcceptanceRateDeltaPercent</c>, <c>PhytosanitaryEfficiencyPercent</c>,
///     <c>PhytosanitaryStatus</c>, and <c>ZonalRisks</c> — none of these have a
///     domain source yet, so asserting they are null/empty confirms the
///     documented "no fabricated data" contract, not a coverage gap.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class SpecialistDashboardQueryServiceTests
{
    private readonly IInterventionRequestRepository _interventionRequestRepository =
        Substitute.For<IInterventionRequestRepository>();
    private readonly IServiceProposalRepository _serviceProposalRepository =
        Substitute.For<IServiceProposalRepository>();
    private readonly IClock _clock = new FakeClock();
    private readonly SpecialistDashboardQueryService _sut;

    public SpecialistDashboardQueryServiceTests()
    {
        _sut = new SpecialistDashboardQueryService(
            _interventionRequestRepository,
            _serviceProposalRepository,
            _clock);
    }

    private static InterventionRequest BuildRequest(
        int id, int specialistId, int growerId, long plotId, string reason,
        InterventionStatus status, DateTimeOffset? createdAt = null)
    {
        var request = new InterventionRequest(
            growerId, plotId, specialistId, alertId: null, reason: reason, message: "msg");

        var idField = typeof(InterventionRequest).GetField(
            "<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        idField!.SetValue(request, id);

        if (status != InterventionStatus.PENDING)
        {
            var statusProperty = typeof(InterventionRequest).GetProperty(nameof(InterventionRequest.Status));
            statusProperty!.SetValue(request, status);
        }

        request.CreatedAt = createdAt;
        return request;
    }

    private static ServiceProposal BuildProposal(
        int id, int interventionRequestId, int specialistId, DateOnly proposedDate,
        ServiceProposalStatus status)
    {
        var proposal = new ServiceProposal(
            interventionRequestId,
            specialistId,
            serviceTitle: "Pest control",
            durationLabel: "2 hours",
            scope: new List<string> { "Inspect", "Treat" },
            proposedDate: proposedDate,
            costEstimate: new CostEstimate(100m, "USD"),
            proposalDetails: "Standard treatment plan");

        var idField = typeof(ServiceProposal).GetField(
            "<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        idField!.SetValue(proposal, id);

        if (status == ServiceProposalStatus.ACCEPTED)
        {
            proposal.Accept();
        }
        else if (status == ServiceProposalStatus.REJECTED)
        {
            proposal.Reject();
        }

        return proposal;
    }

    private void ArrangeRepositories(
        int specialistId,
        IReadOnlyList<InterventionRequest> pending,
        IReadOnlyList<InterventionRequest> accepted,
        IReadOnlyList<ServiceProposal> proposals)
    {
        _interventionRequestRepository
            .FindBySpecialistIdAndStatusAsync(specialistId, InterventionStatus.PENDING, Arg.Any<CancellationToken>())
            .Returns(pending);
        _interventionRequestRepository
            .FindBySpecialistIdAndStatusAsync(specialistId, InterventionStatus.ACCEPTED, Arg.Any<CancellationToken>())
            .Returns(accepted);
        _serviceProposalRepository
            .FindBySpecialistIdAsync(specialistId, Arg.Any<CancellationToken>())
            .Returns(proposals);
    }

    /// <summary>
    ///     GIVEN 3 accepted requests for the specialist
    ///     WHEN the dashboard is handled
    ///     THEN ResolvedInterventions equals the accepted-request count.
    /// </summary>
    [Fact]
    public async Task Handle_ResolvedInterventions_EqualsAcceptedRequestCount()
    {
        // GIVEN 3 accepted requests, 0 pending, 0 proposals
        var accepted = new List<InterventionRequest>
        {
            BuildRequest(1, 5, 1, 1, "r1", InterventionStatus.ACCEPTED),
            BuildRequest(2, 5, 2, 2, "r2", InterventionStatus.ACCEPTED),
            BuildRequest(3, 5, 3, 3, "r3", InterventionStatus.ACCEPTED),
        };
        ArrangeRepositories(5, pending: Array.Empty<InterventionRequest>(), accepted: accepted,
            proposals: Array.Empty<ServiceProposal>());

        var query = new GetSpecialistDashboardQuery(SpecialistId: 5);

        // WHEN the dashboard is fetched
        var resource = await _sut.Handle(query, CancellationToken.None);

        // THEN ResolvedInterventions matches
        Assert.Equal(3, resource.ResolvedInterventions);
    }

    /// <summary>
    ///     GIVEN a known mix of decided service proposals (2 accepted, 1 rejected, 1 still pending)
    ///     WHEN the dashboard is handled
    ///     THEN AcceptanceRatePercent = round(accepted * 100 / decided, 1), where decided
    ///     excludes PENDING proposals (2 * 100 / 3 = 66.7).
    /// </summary>
    [Fact]
    public async Task Handle_AcceptanceRate_ComputesFromDecidedProposalsOnly()
    {
        // GIVEN 2 accepted, 1 rejected, 1 pending proposal
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var proposals = new List<ServiceProposal>
        {
            BuildProposal(1, 1, 5, today, ServiceProposalStatus.ACCEPTED),
            BuildProposal(2, 2, 5, today, ServiceProposalStatus.ACCEPTED),
            BuildProposal(3, 3, 5, today, ServiceProposalStatus.REJECTED),
            BuildProposal(4, 4, 5, today, ServiceProposalStatus.PENDING),
        };
        ArrangeRepositories(5, pending: Array.Empty<InterventionRequest>(),
            accepted: Array.Empty<InterventionRequest>(), proposals: proposals);

        var query = new GetSpecialistDashboardQuery(SpecialistId: 5);

        // WHEN the dashboard is fetched
        var resource = await _sut.Handle(query, CancellationToken.None);

        // THEN the rate is 66.7% (2 accepted / 3 decided, PENDING excluded from the denominator)
        Assert.Equal(66.7, resource.AcceptanceRatePercent);
    }

    /// <summary>
    ///     GIVEN the specialist has zero decided proposals (either no proposals at all,
    ///     or only PENDING ones)
    ///     WHEN the dashboard is handled
    ///     THEN AcceptanceRatePercent is null — the div-by-zero edge case is guarded
    ///     explicitly so the client shows an empty state instead of a misleading 0%.
    /// </summary>
    [Fact]
    public async Task Handle_AcceptanceRate_ZeroDecidedProposals_ReturnsNull_DivByZeroGuard()
    {
        // GIVEN no proposals at all
        ArrangeRepositories(5, pending: Array.Empty<InterventionRequest>(),
            accepted: Array.Empty<InterventionRequest>(), proposals: Array.Empty<ServiceProposal>());

        var query = new GetSpecialistDashboardQuery(SpecialistId: 5);

        // WHEN the dashboard is fetched
        var resource = await _sut.Handle(query, CancellationToken.None);

        // THEN the rate is null, not a divide-by-zero exception or a fabricated 0
        Assert.Null(resource.AcceptanceRatePercent);
    }

    /// <summary>
    ///     GIVEN pending requests for the specialist
    ///     WHEN the dashboard is handled
    ///     THEN each is mapped to an IncomingRequest with the documented field shape:
    ///     ReferenceCode="REQ-{Id}", PlotLabel="Plot #{PlotId}", GrowerLabel="Grower #{GrowerId}",
    ///     Problem=Reason, CreatedAt=request.CreatedAt (ISO "O" format).
    /// </summary>
    [Fact]
    public async Task Handle_IncomingRequests_MapsPendingRequestsToInboxShape()
    {
        // GIVEN one pending request with a known CreatedAt
        var createdAt = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var pending = new List<InterventionRequest>
        {
            BuildRequest(42, 5, growerId: 7, plotId: 99, reason: "Aphid infestation",
                status: InterventionStatus.PENDING, createdAt: createdAt),
        };
        ArrangeRepositories(5, pending: pending, accepted: Array.Empty<InterventionRequest>(),
            proposals: Array.Empty<ServiceProposal>());

        var query = new GetSpecialistDashboardQuery(SpecialistId: 5);

        // WHEN the dashboard is fetched
        var resource = await _sut.Handle(query, CancellationToken.None);

        // THEN the incoming request is shaped as documented
        var incoming = Assert.Single(resource.IncomingRequests);
        Assert.Equal(42, incoming.Id);
        Assert.Equal("REQ-42", incoming.ReferenceCode);
        Assert.Equal("Plot #99", incoming.PlotLabel);
        Assert.Equal("Grower #7", incoming.GrowerLabel);
        Assert.Equal("Aphid infestation", incoming.Problem);
        Assert.Equal(createdAt.ToString("O"), incoming.CreatedAt);
    }

    /// <summary>
    ///     GIVEN accepted proposals dated across several months of the current year
    ///     WHEN the dashboard is handled
    ///     THEN the 12-point monthly series buckets each accepted proposal into its
    ///     ProposedDate's month, and non-accepted / other-year proposals are excluded.
    /// </summary>
    [Fact]
    public async Task Handle_MonthlySeries_BucketsAcceptedProposalsByMonth()
    {
        // GIVEN 2 accepted proposals in January (current year), 1 in March, 1 rejected in January
        var year = DateTime.UtcNow.Year;
        var proposals = new List<ServiceProposal>
        {
            BuildProposal(1, 1, 5, new DateOnly(year, 1, 10), ServiceProposalStatus.ACCEPTED),
            BuildProposal(2, 2, 5, new DateOnly(year, 1, 20), ServiceProposalStatus.ACCEPTED),
            BuildProposal(3, 3, 5, new DateOnly(year, 3, 5), ServiceProposalStatus.ACCEPTED),
            BuildProposal(4, 4, 5, new DateOnly(year, 1, 15), ServiceProposalStatus.REJECTED),
            // Different year — must not be counted in the monthly series (year-scoped).
            BuildProposal(5, 5, 5, new DateOnly(year - 1, 1, 10), ServiceProposalStatus.ACCEPTED),
        };
        ArrangeRepositories(5, pending: Array.Empty<InterventionRequest>(),
            accepted: Array.Empty<InterventionRequest>(), proposals: proposals);

        var query = new GetSpecialistDashboardQuery(SpecialistId: 5);

        // WHEN the dashboard is fetched
        var resource = await _sut.Handle(query, CancellationToken.None);

        // THEN the series has 12 points, JAN=2, MAR=1, all other months=0
        Assert.Equal(12, resource.PerformanceMonthly.Count);
        Assert.Equal("JAN", resource.PerformanceMonthly[0].Label);
        Assert.Equal(2, resource.PerformanceMonthly[0].Value);
        Assert.Equal("MAR", resource.PerformanceMonthly[2].Label);
        Assert.Equal(1, resource.PerformanceMonthly[2].Value);
        Assert.All(
            resource.PerformanceMonthly.Where(p => p.Label != "JAN" && p.Label != "MAR"),
            p => Assert.Equal(0, p.Value));
    }

    /// <summary>
    ///     GIVEN accepted proposals dated across the last 4 years (current year and 3 prior)
    ///     WHEN the dashboard is handled
    ///     THEN the annual series has exactly 4 points labeled currentYear-3..currentYear,
    ///     each counting only that year's accepted proposals.
    /// </summary>
    [Fact]
    public async Task Handle_AnnualSeries_BucketsAcceptedProposalsByYear_LastFourYears()
    {
        // GIVEN 1 accepted proposal in each of the last 4 years, plus 1 accepted 5 years back
        // (out of the 4-year window) which must NOT appear in any bucket.
        var currentYear = DateTime.UtcNow.Year;
        var proposals = new List<ServiceProposal>
        {
            BuildProposal(1, 1, 5, new DateOnly(currentYear - 3, 6, 1), ServiceProposalStatus.ACCEPTED),
            BuildProposal(2, 2, 5, new DateOnly(currentYear - 2, 6, 1), ServiceProposalStatus.ACCEPTED),
            BuildProposal(3, 3, 5, new DateOnly(currentYear - 1, 6, 1), ServiceProposalStatus.ACCEPTED),
            BuildProposal(4, 4, 5, new DateOnly(currentYear, 6, 1), ServiceProposalStatus.ACCEPTED),
            BuildProposal(5, 5, 5, new DateOnly(currentYear, 7, 1), ServiceProposalStatus.ACCEPTED),
            BuildProposal(6, 6, 5, new DateOnly(currentYear - 5, 6, 1), ServiceProposalStatus.ACCEPTED),
        };
        ArrangeRepositories(5, pending: Array.Empty<InterventionRequest>(),
            accepted: Array.Empty<InterventionRequest>(), proposals: proposals);

        var query = new GetSpecialistDashboardQuery(SpecialistId: 5);

        // WHEN the dashboard is fetched
        var resource = await _sut.Handle(query, CancellationToken.None);

        // THEN exactly 4 points, labeled currentYear-3..currentYear, current year has 2
        Assert.Equal(4, resource.PerformanceAnnual.Count);
        Assert.Equal((currentYear - 3).ToString(), resource.PerformanceAnnual[0].Label);
        Assert.Equal(1, resource.PerformanceAnnual[0].Value);
        Assert.Equal((currentYear - 2).ToString(), resource.PerformanceAnnual[1].Label);
        Assert.Equal(1, resource.PerformanceAnnual[1].Value);
        Assert.Equal((currentYear - 1).ToString(), resource.PerformanceAnnual[2].Label);
        Assert.Equal(1, resource.PerformanceAnnual[2].Value);
        Assert.Equal(currentYear.ToString(), resource.PerformanceAnnual[3].Label);
        Assert.Equal(2, resource.PerformanceAnnual[3].Value);
    }

    /// <summary>
    ///     GIVEN any dashboard request
    ///     WHEN the dashboard is handled
    ///     THEN the 4 fields with no domain source yet are null/empty BY DESIGN
    ///     (documented on <see cref="SpecialistDashboardQueryService"/>'s class remarks):
    ///     AcceptanceRateDeltaPercent (needs a historical snapshot not persisted yet),
    ///     PhytosanitaryEfficiencyPercent and PhytosanitaryStatus (no intervention-outcome
    ///     read model yet), and ZonalRisks (no geospatial surveillance projection yet).
    ///     This is the correct, expected empty state — NOT a coverage gap.
    /// </summary>
    [Fact]
    public async Task Handle_UnimplementedFields_AreNullOrEmpty_ByDesign()
    {
        // GIVEN a specialist with some real data present (proves the empty fields are
        // independent of the real ones being populated)
        var accepted = new List<InterventionRequest>
        {
            BuildRequest(1, 5, 1, 1, "r1", InterventionStatus.ACCEPTED),
        };
        var proposals = new List<ServiceProposal>
        {
            BuildProposal(1, 1, 5, DateOnly.FromDateTime(DateTime.UtcNow), ServiceProposalStatus.ACCEPTED),
        };
        ArrangeRepositories(5, pending: Array.Empty<InterventionRequest>(), accepted: accepted, proposals: proposals);

        var query = new GetSpecialistDashboardQuery(SpecialistId: 5);

        // WHEN the dashboard is fetched
        var resource = await _sut.Handle(query, CancellationToken.None);

        // THEN the 4 by-design fields are null/empty
        Assert.Null(resource.AcceptanceRateDeltaPercent);
        Assert.Null(resource.PhytosanitaryEfficiencyPercent);
        Assert.Null(resource.PhytosanitaryStatus);
        Assert.Empty(resource.ZonalRisks);
    }

    /// <summary>
    ///     GIVEN the injected clock is frozen at a known instant
    ///     WHEN the dashboard is handled
    ///     THEN UpdatedAt (the resource-level "as of" timestamp) reflects that instant,
    ///     confirming the resource's freshness stamp is clock-derived, not wall-clock.
    /// </summary>
    [Fact]
    public async Task Handle_UpdatedAt_ReflectsInjectedClock()
    {
        // GIVEN a clock frozen at a known instant
        var seed = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FakeClock(seed);
        var sut = new SpecialistDashboardQueryService(_interventionRequestRepository, _serviceProposalRepository, clock);
        ArrangeRepositories(5, pending: Array.Empty<InterventionRequest>(),
            accepted: Array.Empty<InterventionRequest>(), proposals: Array.Empty<ServiceProposal>());

        var query = new GetSpecialistDashboardQuery(SpecialistId: 5);

        // WHEN the dashboard is fetched
        var resource = await sut.Handle(query, CancellationToken.None);

        // THEN UpdatedAt matches the clock instant, in ISO "O" format
        var expected = new DateTimeOffset(seed, TimeSpan.Zero).ToString("O");
        Assert.Equal(expected, resource.UpdatedAt);
    }
}
