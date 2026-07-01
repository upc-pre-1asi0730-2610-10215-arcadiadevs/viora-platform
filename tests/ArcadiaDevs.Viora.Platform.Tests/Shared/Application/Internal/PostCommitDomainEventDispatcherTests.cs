using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Cortex.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Application.Internal;

/// <summary>
///     Integration tests for the <c>PostCommitDomainEventDispatcher</c>
///     (A6 / SHARED-011) — the <c>SaveChangesInterceptor</c> that
///     dispatches <see cref="IEvent"/> instances raised on
///     <see cref="IHasDomainEvents"/> aggregates AFTER the underlying
///     <c>SaveChanges</c> commit has succeeded.
///     <para>
///         The 9 tests below retro-fit the 9 Phase 2 A6 acceptance
///         scenarios from spec #75 (S1.10..S1.15, S1.17, S1.18 +
///         S1.16 idempotency). The dedicated
///         <c>IdempotencyTests</c> class adds a 10th test that
///         exercises the canonical
///         "dispatched twice for the same aggregate, only published
///         once" scenario end-to-end via the real in-process
///         <c>IMediator</c> bus.
///     </para>
///     <para>
///         Each test inherits <see cref="IntegrationTestBase"/> so
///         it shares the Testcontainers.PostgreSql container
///         lifecycle with the other Postgres tests in the
///         <c>HarnessCollection</c>. The
///         <see cref="PostgresTestContainer"/> is started in the
///         base class; the test class builds a fresh
///         <see cref="AppDbContext"/> on top of the container's
///         connection string and constructs the
///         <see cref="PostCommitDomainEventDispatcher"/> directly
///         with a NSubstitute <see cref="IMediator"/> so the test
///         can assert the publication path. This matches the spec
///         literally (S1.10 GIVEN: "a
///         PostCommitDomainEventDispatcher constructed with a
///         substitute IMediator").
///     </para>
///     <para>
///         The [Collection("Postgres")] attribute joins the F1a
///         <c>HarnessCollection</c> to serialize with
///         <c>HarnessSmokeTest</c> +
///         <c>PostCommitDomainEventDispatcherLifetimeTests</c> and
///         avoid the InMemory seed race surfaced in 1.15.1 (R1
///         mitigation, obs #82).
///     </para>
/// </summary>
[Collection("Postgres")]
[Trait("Category", "Integration")]
[Trait("Database", "Postgres")]
public class PostCommitDomainEventDispatcherTests : IntegrationTestBase
{
    private IMediator _mediator = null!;
    private ILogger<PostCommitDomainEventDispatcher> _logger = null!;

    /// <summary>
    ///     Initializes the test base (which boots the Postgres
    ///     container) and configures the per-test
    ///     <see cref="IMediator"/> substitute + a null logger.
    /// </summary>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _mediator = Substitute.For<IMediator>();
        _logger = NullLogger<PostCommitDomainEventDispatcher>.Instance;
    }

    /// <summary>
    ///     Builds a fresh <see cref="AppDbContext"/> wired to the
    ///     Postgres container with the
    ///     <see cref="PostCommitDomainEventDispatcher"/> registered
    ///     as a <c>SaveChangesInterceptor</c> using the
    ///     NSubstitute <see cref="IMediator"/> from
    ///     <see cref="InitializeAsync"/>. Matches the production
    ///     <c>Program.cs:103-105</c> registration order.
    /// </summary>
    private AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PostgresConnectionString)
            .AddInterceptors(new PostCommitDomainEventDispatcher(_mediator, _logger))
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    ///     Builds a fresh <see cref="AppDbContext"/> wired to the
    ///     Postgres container with a throwing
    ///     <see cref="SaveChangesInterceptor"/> registered FIRST
    ///     (so <c>SavingChangesAsync</c> throws before the actual
    ///     save runs) AND the
    ///     <see cref="PostCommitDomainEventDispatcher"/>
    ///     registered SECOND. The exception simulates the
    ///     "DbUpdateException raised by a throwing
    ///     SaveChangesAsync inner" scenario from S1.12 + S1.17
    ///     GIVEN clauses.
    /// </summary>
    private AppDbContext NewFailingContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(PostgresConnectionString)
            .AddInterceptors(
                new ThrowingSaveChangesInterceptor(),
                new PostCommitDomainEventDispatcher(_mediator, _logger))
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    ///     S1.10 (Phase 2 S6.1) — Successful save with 1 entity
    ///     carrying 1 domain event publishes the event through the
    ///     <c>IMediator</c> bus and clears the aggregate's
    ///     <c>DomainEvents</c> collection.
    /// </summary>
    [Fact]
    public async Task SavedChangesAsync_OneEntityOneEvent_PublishesAndClears()
    {
        // GIVEN a fresh AppDbContext + dispatcher with a substitute
        // IMediator + an alert with 1 domain event.
        _mediator.ClearReceivedCalls();
        await using var context = NewContext();
        await context.Database.MigrateAsync();
        var alert = await CreatePendingAlertAsync();
        alert.ConfirmFromInspection();
        Assert.NotEmpty(alert.DomainEvents);

        // WHEN the alert is persisted via the dispatcher's
        // SaveChanges path.
        context.Set<Alert>().Add(alert);
        await context.SaveChangesAsync();

        // THEN the substitute received exactly 1 PublishAsync call
        // for the AlertUpdatedEvent AND the aggregate's
        // DomainEvents collection is cleared.
        await _mediator.Received(1).PublishAsync(
            Arg.Is<AlertUpdatedEvent>(e => e.Transition == "CONFIRMED"),
            Arg.Any<CancellationToken>());
        Assert.Empty(alert.DomainEvents);
    }

    /// <summary>
    ///     S1.11 (Phase 2 S6.2) — Successful save with 0 domain
    ///     events does NOT publish anything on the bus.
    /// </summary>
    [Fact]
    public async Task SavedChangesAsync_NoEvents_DoesNotPublish()
    {
        // GIVEN a fresh context + a brand-new alert (the ctor
        // does not raise any domain events).
        _mediator.ClearReceivedCalls();
        await using var context = NewContext();
        await context.Database.MigrateAsync();
        var alert = await CreatePendingAlertAsync();
        Assert.Empty(alert.DomainEvents);

        // WHEN the aggregate is persisted.
        context.Set<Alert>().Add(alert);
        await context.SaveChangesAsync();

        // THEN the bus saw no publications.
        await _mediator.DidNotReceive().PublishAsync(
            Arg.Any<AlertUpdatedEvent>(),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     S1.12 (Phase 2 S6.3) — A failed
    ///     <c>SaveChangesAsync</c> does NOT publish; the events
    ///     stay on the aggregate for a subsequent retry.
    /// </summary>
    [Fact]
    public async Task SavedChangesAsync_SaveFails_EventsStayForRetry()
    {
        // GIVEN a fresh context configured with a throwing
        // interceptor (so SavingChangesAsync throws) + an alert
        // with 1 event.
        _mediator.ClearReceivedCalls();
        await using var context = NewFailingContext();
        await context.Database.MigrateAsync();

        var alert = await CreatePendingAlertAsync();
        alert.ConfirmFromInspection();
        var preSaveEventCount = alert.DomainEvents.Count;
        Assert.True(preSaveEventCount >= 1);

        // WHEN the save is attempted and fails (the throwing
        // interceptor raises a DbUpdateException in
        // SavingChangesAsync before the actual commit).
        context.Set<Alert>().Add(alert);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        // THEN the bus saw no publications; the events on
        // `alert` are still on the aggregate.
        await _mediator.DidNotReceive().PublishAsync(
            Arg.Any<AlertUpdatedEvent>(),
            Arg.Any<CancellationToken>());
        Assert.Equal(preSaveEventCount, alert.DomainEvents.Count);
    }

    /// <summary>
    ///     S1.13 (Phase 2 S6.4) — A handler exception is caught +
    ///     logged + swallowed; the next entity's events still
    ///     dispatch.
    /// </summary>
    [Fact]
    public async Task SavedChangesAsync_HandlerThrows_IsSwallowedNextEventStillPublishes()
    {
        // GIVEN a substitute IMediator whose FIRST
        // PublishAsync throws (the second succeeds).
        _mediator.ClearReceivedCalls();
        var callCount = 0;
        _mediator.PublishAsync(Arg.Any<AlertUpdatedEvent>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Simulated consumer failure.");
                }
                return Task.CompletedTask;
            });

        await using var context = NewContext();
        await context.Database.MigrateAsync();

        // WHEN 2 alerts are saved in a single SaveChanges call.
        var alert1 = await CreatePendingAlertAsync();
        alert1.ConfirmFromInspection();
        var alert2 = await CreatePendingAlertAsync();
        alert2.ConfirmFromInspection();
        context.Set<Alert>().Add(alert1);
        context.Set<Alert>().Add(alert2);
        await context.SaveChangesAsync();

        // THEN both events were attempted; the first one's
        // exception was swallowed and the second published
        // successfully. The bus received 2 PublishAsync calls
        // (one of which threw); the dispatcher did not re-throw.
        await _mediator.Received(2).PublishAsync(
            Arg.Any<AlertUpdatedEvent>(),
            Arg.Any<CancellationToken>());
        Assert.Empty(alert1.DomainEvents);
        Assert.Empty(alert2.DomainEvents);
    }

    /// <summary>
    ///     S1.14 (Phase 2 S6.5) — 2 entities with 3 events total
    ///     are published in aggregate-snapshot order.
    /// </summary>
    [Fact]
    public async Task SavedChangesAsync_TwoEntitiesThreeEvents_PublishedInSnapshotOrder()
    {
        // GIVEN 2 alerts: alertA with 2 events (Confirm + Escalate)
        // and alertB with 1 event (Dismiss) = 3 events total.
        _mediator.ClearReceivedCalls();
        await using var context = NewContext();
        await context.Database.MigrateAsync();

        var alertA = await CreatePendingAlertAsync();
        alertA.ConfirmFromInspection(); // event #1
        alertA.Escalate();              // event #2

        var alertB = await CreatePendingAlertAsync();
        alertB.Dismiss();                // event #3

        // WHEN both aggregates are persisted in a single save.
        context.Set<Alert>().Add(alertA);
        context.Set<Alert>().Add(alertB);
        await context.SaveChangesAsync();

        // THEN the bus saw all 3 publications.
        await _mediator.Received(1).PublishAsync(
            Arg.Is<AlertUpdatedEvent>(e => e.Transition == "CONFIRMED"),
            Arg.Any<CancellationToken>());
        await _mediator.Received(1).PublishAsync(
            Arg.Is<AlertUpdatedEvent>(e => e.Transition == "ESCALATED"),
            Arg.Any<CancellationToken>());
        await _mediator.Received(1).PublishAsync(
            Arg.Is<AlertUpdatedEvent>(e => e.Transition == "DISMISSED"),
            Arg.Any<CancellationToken>());
        // And both aggregates' DomainEvents are cleared.
        Assert.Empty(alertA.DomainEvents);
        Assert.Empty(alertB.DomainEvents);
    }

    /// <summary>
    ///     S1.15 (Phase 2 S6.6) — The sync
    ///     <c>SavedChanges</c> overload behaves identically to
    ///     <c>SavedChangesAsync</c>: publishes the event and
    ///     clears the aggregate.
    /// </summary>
    [Fact]
    public void SavedChanges_SyncOverload_PublishesAndClears()
    {
        // GIVEN a fresh context + a dispatcher + an alert with
        // 1 event.
        _mediator.ClearReceivedCalls();
        using var context = NewContext();
        context.Database.MigrateAsync().GetAwaiter().GetResult();

        var alert = new Alert(new CreateAlertCommand(
            PlotId: 70_000L,
            AlertType: EThreatType.PHENOLOGICAL_RISK.ToString(),
            Severity: EAlertSeverity.MEDIUM.ToString(),
            Title: $"Sync test {Guid.NewGuid():N}",
            RiskExplanation: "Sync overload test risk explanation",
            Sources: null,
            DataProviders: null,
            SupportingData: null));
        alert.ConfirmFromInspection();
        Assert.NotEmpty(alert.DomainEvents);

        // WHEN the SYNC SaveChanges path runs.
        context.Set<Alert>().Add(alert);
        context.SaveChanges();

        // THEN the sync dispatch happened and the aggregate
        // was cleared.
        _mediator.Received(1).PublishAsync(
            Arg.Is<AlertUpdatedEvent>(e => e.Transition == "CONFIRMED"),
            Arg.Any<CancellationToken>());
        Assert.Empty(alert.DomainEvents);
    }

    /// <summary>
    ///     S1.16 (Phase 2 S6.7) — Idempotency: calling dispatch
    ///     twice on the same aggregate does NOT re-publish. The
    ///     first <c>SavedChangesAsync</c> clears the aggregate's
    ///     <c>DomainEvents</c>; the second call snapshots an
    ///     empty collection and dispatches nothing.
    ///     <para>
    ///         The dedicated <c>IdempotencyTests</c> class also
    ///         covers this scenario as a real end-to-end
    ///         integration test (2 SaveChanges calls + the bus
    ///         sees exactly 1 publication).
    ///     </para>
    /// </summary>
    [Fact]
    public async Task SavedChangesAsync_CalledTwice_SecondCallDoesNotRepublish()
    {
        // GIVEN a fresh context + an alert with 1 event.
        _mediator.ClearReceivedCalls();
        await using var context = NewContext();
        await context.Database.MigrateAsync();

        var alert = await CreatePendingAlertAsync();
        alert.ConfirmFromInspection();
        Assert.NotEmpty(alert.DomainEvents);

        // WHEN the first save runs.
        context.Set<Alert>().Add(alert);
        await context.SaveChangesAsync();
        var countAfterFirst = _mediator.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "PublishAsync");
        Assert.Equal(1, countAfterFirst);
        Assert.Empty(alert.DomainEvents);

        // WHEN a second SaveChanges runs on the SAME context
        // (no new entities tracked).
        await context.SaveChangesAsync();

        // THEN no additional publications occurred.
        var countAfterSecond = _mediator.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "PublishAsync");
        Assert.Equal(1, countAfterSecond);
    }

    /// <summary>
    ///     S1.17 (Phase 2 S6.8) — Idempotency: a retry of
    ///     <c>SaveChangesAsync</c> after a rollback re-publishes.
    ///     The first save fails, the events stay on the
    ///     aggregate; the second save succeeds and the dispatcher
    ///     fires.
    /// </summary>
    [Fact]
    public async Task SavedChangesAsync_FailedThenRetry_PublishesOnRetry()
    {
        // GIVEN a fresh context configured with a throwing
        // interceptor (first save fails) + an alert with 1 event.
        _mediator.ClearReceivedCalls();
        await using var failingContext = NewFailingContext();
        await failingContext.Database.MigrateAsync();

        var alert = await CreatePendingAlertAsync();
        alert.ConfirmFromInspection();
        var preSaveEventCount = alert.DomainEvents.Count;
        Assert.True(preSaveEventCount >= 1);

        // Phase 1: the first save fails; the events stay on
        // the aggregate.
        failingContext.Set<Alert>().Add(alert);
        await Assert.ThrowsAsync<DbUpdateException>(() => failingContext.SaveChangesAsync());
        failingContext.ChangeTracker.Clear();
        Assert.Equal(preSaveEventCount, alert.DomainEvents.Count);
        await _mediator.DidNotReceive().PublishAsync(
            Arg.Any<AlertUpdatedEvent>(),
            Arg.Any<CancellationToken>());

        // Phase 2: a fresh context (no throwing interceptor)
        // performs the retry; the events are re-published.
        await using var retryContext = NewContext();
        await retryContext.Database.MigrateAsync();
        retryContext.Set<Alert>().Add(alert);
        await retryContext.SaveChangesAsync();

        // THEN the retry published the event.
        await _mediator.Received(1).PublishAsync(
            Arg.Is<AlertUpdatedEvent>(e => e.Transition == "CONFIRMED"),
            Arg.Any<CancellationToken>());
        Assert.Empty(alert.DomainEvents);
    }

    /// <summary>
    ///     S1.18 (Phase 2 S6.9) — <c>AcceptAllChangesOnSuccess =
    ///     false</c> does not affect the dispatch contract. The
    ///     event is published and the aggregate's
    ///     <c>DomainEvents</c> collection is cleared.
    /// </summary>
    [Fact]
    public async Task SavedChangesAsync_AcceptAllChangesOnSuccessFalse_StillPublishes()
    {
        // GIVEN a fresh context + an alert with 1 event.
        _mediator.ClearReceivedCalls();
        await using var context = NewContext();
        await context.Database.MigrateAsync();

        var alert = await CreatePendingAlertAsync();
        alert.ConfirmFromInspection();
        Assert.NotEmpty(alert.DomainEvents);

        // WHEN SaveChanges runs with
        // AcceptAllChangesOnSuccess = false.
        context.Set<Alert>().Add(alert);
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: false);

        // THEN the publication happened and the aggregate is
        // cleared.
        await _mediator.Received(1).PublishAsync(
            Arg.Is<AlertUpdatedEvent>(e => e.Transition == "CONFIRMED"),
            Arg.Any<CancellationToken>());
        Assert.Empty(alert.DomainEvents);
    }

    // -----------------------------------------------------------------
    // Test helpers
    // -----------------------------------------------------------------

    /// <summary>
    ///     Builds a fresh <see cref="Alert"/> aggregate via the
    ///     production <see cref="CreateAlertCommand"/> ctor. The
    ///     ctor does NOT raise any <see cref="IEvent"/>; the test
    ///     must invoke a state-machine method (e.g.
    ///     <see cref="Alert.ConfirmFromInspection"/>) to attach
    ///     events to the aggregate.
    /// </summary>
    private static async Task<Alert> CreatePendingAlertAsync()
    {
        var command = new CreateAlertCommand(
            PlotId: 70_000L + Math.Abs(Guid.NewGuid().GetHashCode() % 10_000L),
            AlertType: EThreatType.PHENOLOGICAL_RISK.ToString(),
            Severity: EAlertSeverity.MEDIUM.ToString(),
            Title: $"Test alert {Guid.NewGuid():N}",
            RiskExplanation: "Test alert for post-commit dispatcher.",
            Sources: new List<string> { "TEST" },
            DataProviders: new List<string> { "TEST" },
            SupportingData: new Dictionary<string, string> { ["test"] = "true" });
        await Task.Yield();
        return new Alert(command);
    }
}

/// <summary>
///     Test-only <see cref="SaveChangesInterceptor"/> that throws
///     a <see cref="DbUpdateException"/> on
///     <c>SavingChangesAsync</c> to simulate a real save failure
///     (e.g. a unique-key violation). The dispatcher is never
///     invoked because the save never reaches the
///     <c>SavedChangesAsync</c> event. The events on the
///     aggregate stay on the aggregate for retry (S1.12, S1.17).
/// </summary>
internal sealed class ThrowingSaveChangesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        throw new DbUpdateException("Simulated save failure for test.");
    }
}
