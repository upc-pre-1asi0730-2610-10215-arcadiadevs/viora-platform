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
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Application.Internal;

/// <summary>
///     Idempotency test for the
///     <c>PostCommitDomainEventDispatcher</c> — exercises the
///     canonical "dispatched twice for the same aggregate, only
///     published once" scenario (Phase 2 S6.7) end-to-end through
///     the production dispatcher's snapshot-then-clear contract.
///     <para>
///         The aggregate carries 1 domain event; the first
///         <c>SaveChangesAsync</c> publishes it via the
///         dispatcher (and clears the aggregate's
///         <c>DomainEvents</c>). A second
///         <c>SaveChangesAsync</c> on the SAME context with NO
///         new tracked entities snapshots an empty
///         <c>DomainEvents</c> collection and dispatches nothing.
///         The test asserts the bus saw exactly 1 publication
///         across the 2 saves.
///     </para>
///     <para>
///         The [Collection("Postgres")] attribute joins the F1a
///         <c>HarnessCollection</c> to serialize with
///         <c>HarnessSmokeTest</c> +
///         <c>PostCommitDomainEventDispatcherLifetimeTests</c> +
///         <c>PostCommitDomainEventDispatcherTests</c> and avoid
///         the InMemory seed race surfaced in 1.15.1 (R1
///         mitigation, obs #82).
///     </para>
/// </summary>
[Collection("Postgres")]
[Trait("Category", "Integration")]
[Trait("Database", "Postgres")]
public class IdempotencyTests : IntegrationTestBase
{
    private IMediator _mediator = null!;
    private string _connectionString = null!;

    /// <summary>
    ///     Initializes the test base (boots the Postgres
    ///     container), stores the connection string for the
    ///     per-test DbContext factory, and configures the
    ///     NSubstitute <see cref="IMediator"/>.
    /// </summary>
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _mediator = Substitute.For<IMediator>();
        _connectionString = PostgresConnectionString;
    }

    /// <summary>
    ///     The canonical "dispatched twice for the same aggregate,
    ///     only dispatched once" idempotency scenario.
    /// </summary>
    [Fact]
    public async Task PostCommitDomainEventDispatcher_DispatchedTwiceForSameAggregate_OnlyDispatchesOnce()
    {
        // GIVEN a fresh AppDbContext with the dispatcher wired as
        // a SaveChangesInterceptor + an alert with 1 domain
        // event.
        _mediator.ClearReceivedCalls();
        await using var context = NewContext();
        await context.Database.MigrateAsync();
        var alert = await CreatePendingAlertAsync();
        alert.ConfirmFromInspection();
        Assert.NotEmpty(alert.DomainEvents);
        var preSaveEventCount = alert.DomainEvents.Count;
        Assert.True(preSaveEventCount >= 1);

        // WHEN the first save runs (the dispatcher publishes +
        // clears the aggregate's DomainEvents).
        context.Set<Alert>().Add(alert);
        await context.SaveChangesAsync();
        var countAfterFirst = _mediator.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "PublishAsync");
        Assert.Equal(1, countAfterFirst);
        Assert.Empty(alert.DomainEvents);

        // AND a second save runs on the same context (no new
        // tracked entities, no pending events).
        await context.SaveChangesAsync();

        // THEN the bus saw exactly 1 publication total — the
        // second save dispatched nothing because the aggregate's
        // DomainEvents collection is empty (the dispatcher
        // cleared it after the first dispatch per the
        // snapshot-then-clear contract).
        var countAfterSecond = _mediator.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == "PublishAsync");
        Assert.Equal(1, countAfterSecond);
        Assert.Empty(alert.DomainEvents);
    }

    /// <summary>
    ///     Builds a fresh <see cref="AppDbContext"/> wired to the
    ///     Postgres container with the
    ///     <see cref="PostCommitDomainEventDispatcher"/>
    ///     registered as a <c>SaveChangesInterceptor</c> using
    ///     the NSubstitute <see cref="IMediator"/>. Matches the
    ///     production <c>Program.cs:103-105</c> registration
    ///     order.
    /// </summary>
    private AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(new PostCommitDomainEventDispatcher(_mediator, NullLogger<PostCommitDomainEventDispatcher>.Instance))
            .Options;
        return new AppDbContext(options);
    }

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
            PlotId: 75_000L + Math.Abs(Guid.NewGuid().GetHashCode() % 10_000L),
            AlertType: EThreatType.PHENOLOGICAL_RISK.ToString(),
            Severity: EAlertSeverity.MEDIUM.ToString(),
            Title: $"Idempotency test alert {Guid.NewGuid():N}",
            RiskExplanation: "Idempotency test alert.",
            Sources: new List<string> { "TEST" },
            DataProviders: new List<string> { "TEST" },
            SupportingData: new Dictionary<string, string> { ["test"] = "true" });
        await Task.Yield();
        return new Alert(command);
    }
}
