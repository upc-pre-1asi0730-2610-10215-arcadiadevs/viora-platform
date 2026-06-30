using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using Cortex.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;

/// <summary>
///     <para>
///         Post-commit domain event dispatcher (CC-4 / SHARED-011). Hooks the
///         EF Core <c>SavedChanges</c> / <c>SavedChangesAsync</c> interception
///         points to dispatch <see cref="IEvent"/> instances raised on
///         <see cref="IHasDomainEvents"/> aggregates AFTER the underlying
///         <c>SaveChanges</c> commit has succeeded.
///     </para>
///     <para>
///         <b>Ordering (snapshot-then-commit-then-dispatch):</b> the
///         interceptor snapshots the pending event collections from the
///         <see cref="ChangeTracker"/> BEFORE <c>base.SavedChangesAsync</c>
///         runs, then awaits the commit, then dispatches. The snapshot step
///         is required because EF Core detaches / mutates tracked entities
///         during the commit, which would otherwise yield an
///         <c>InvalidOperationException</c> ("collection was modified")
///         when the aggregate's <c>DomainEvents</c> collection is
///         enumerated post-commit.
///     </para>
///     <para>
///         <b>Best-effort dispatch (CC-9):</b> handler exceptions thrown by
///         <see cref="IMediator.PublishAsync"/> are logged at <c>Error</c>
///         and swallowed. The originating DB write is NOT rolled back; the
///         domain transaction stays committed, and the failed event is
///         effectively lost. This is intentional and locked — coupling the
///         domain write to consumer availability would invert the
///         dependency and make the platform brittle.
///     </para>
///     <para>
///         <b>Sync / async parity:</b> the async overload performs the
///         dispatch via <c>await</c> on the EF Core thread pool continuation;
///         the sync overload delegates to the same dispatch helper via
///         <c>GetAwaiter().GetResult()</c> to block the caller rather than
///         fire-and-forget on <c>Task.Run</c> (the latter would be a
///         sync-over-async antipattern per design OQ #1 in engram #46).
///     </para>
///     <para>
///         <b>Snapshot-only-on-success:</b> the dispatch loop runs only
///         when <c>result &gt; 0</c> (the save actually persisted at least
///         one entity). A no-op save (every tracked entry was unchanged or
///         detached at commit time) leaves the event collections on the
///         aggregates untouched for the next save attempt. A save that
///         throws (e.g. <c>DbUpdateException</c>) propagates the exception
///         and the snapshot is dropped — the events stay on the
///         aggregates, ready to be re-snapshotted on the next attempt.
///     </para>
///     <para>
///         <b>Clear-on-success:</b> after the events have been dispatched
///         (or attempted), the snapshot is released and each aggregate's
///         <c>ClearDomainEvents()</c> is invoked. The
///         <see cref="IHasDomainEvents"/> contract stays read-only; the
///         clear method is a public member of the concrete
///         <see cref="Alert"/> aggregate (and will be added to future
///         aggregates as they adopt the contract). The dispatcher uses a
///         type check to call the concrete method on the only Phase 2
///         implementer (<see cref="Alert"/>); future aggregates will
///         extend the type check as needed.
///     </para>
///     <para>
///         <b>In-process bus (CC-2):</b> the bus is
///         <c>Cortex.Mediator</c>'s in-process notification pipeline; no
///         outbox, no cross-process delivery, process restart loses
///         in-flight events. This is identical to the
///         <c>os-viora-platform</c> Spring <c>afterCommit</c> semantics.
///     </para>
///     <para>
///         <b>Registration order (locked):</b> this interceptor MUST be
///         registered AFTER <see cref="AuditableEntityInterceptor"/> in
///         <c>AddInterceptors(...)</c> so the audit timestamps
///         (<c>CreatedAt</c> / <c>UpdatedAt</c>) are stamped on the entity
///         before the post-commit dispatcher reads the entity state into
///         the event payload. See <c>Program.cs</c>.
///     </para>
/// </summary>
public sealed class PostCommitDomainEventDispatcher : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    private readonly ILogger<PostCommitDomainEventDispatcher> _logger;

    /// <summary>
    ///     Initializes a new instance of the
    ///     <see cref="PostCommitDomainEventDispatcher"/> class. Both
    ///     dependencies are required and are null-checked at construction
    ///     time (the C# 12 primary-ctor pattern requires the null-check
    ///     inside the body, since the primary-ctor body IS the class body).
    /// </summary>
    /// <param name="mediator">
    ///     The in-process <c>Cortex.Mediator</c> bus used to publish each
    ///     <see cref="IEvent"/> to its registered
    ///     <c>IEventHandler&lt;TEvent&gt;</c> consumers.
    /// </param>
    /// <param name="logger">
    ///     Logger for best-effort dispatch failures (CC-9).
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="mediator"/> or
    ///     <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public PostCommitDomainEventDispatcher(
        IMediator mediator,
        ILogger<PostCommitDomainEventDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(logger);

        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        // 1) Snapshot BEFORE the base call. The aggregate's
        //    DomainEvents collection is mutated by the ChangeTracker during
        //    the commit; enumerating it after the commit would race with the
        //    tracker and throw. Snapshotting the events to a local list makes
        //    the dispatch loop safe.
        var pending = SnapshotPendingEvents(eventData.Context);

        // 2) Commit. Awaiting base first ensures the DB write is durable
        //    before any consumer is notified (this is the post-commit
        //    contract: no consumer ever sees an event whose transaction
        //    was rolled back).
        var saved = await base.SavedChangesAsync(eventData, result, cancellationToken);

        // 3) Dispatch only on a successful commit. A no-op save (result
        //    == 0) means the ChangeTracker had no Added/Modified/Deleted
        //    entries to persist, so the events (if any) stay on the
        //    aggregates for the next attempt.
        if (saved > 0)
        {
            await DispatchAndClearAsync(pending, cancellationToken);
        }

        return saved;
    }

    /// <inheritdoc />
    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        // Same snapshot-then-commit-then-dispatch sequence as the async
        // overload. The dispatch helper is async by design (the IMediator
        // surface is async-only), so the sync overload blocks the caller
        // via GetAwaiter().GetResult() rather than firing-and-forgetting
        // on Task.Run (which would be a sync-over-async antipattern per
        // design OQ #1).
        var pending = SnapshotPendingEvents(eventData.Context);

        var saved = base.SavedChanges(eventData, result);

        if (saved > 0)
        {
            DispatchAndClearAsync(pending, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        return saved;
    }

    /// <summary>
    ///     Captures a thread-local snapshot of the
    ///     <see cref="IHasDomainEvents.DomainEvents"/> collection for every
    ///     tracked aggregate that is in a non-<see cref="EntityState.Detached"/>
    ///     state. The snapshot is a tuple of (aggregate, events.ToList())
    ///     so the dispatch loop never re-reads the live collection.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="DbContext"/> provided by EF Core on the
    ///     <c>SavedChanges</c> hook. May be <see langword="null"/> when the
    ///     hook is invoked outside a tracked scope (e.g. unit tests with a
    ///     null <c>UseDbContext</c> factory); a null context returns an
    ///     empty snapshot and the dispatcher is a no-op.
    /// </param>
    private static List<(IHasDomainEvents Aggregate, List<IEvent> Events)> SnapshotPendingEvents(
        DbContext? context)
    {
        if (context is null)
        {
            return new List<(IHasDomainEvents, List<IEvent>)>();
        }

        return context.ChangeTracker
            .Entries()
            .Where(entry => entry.State != EntityState.Detached && entry.Entity is IHasDomainEvents)
            .Select(entry => ((IHasDomainEvents)entry.Entity, entry.Entity is IHasDomainEvents aggregate
                ? aggregate.DomainEvents.ToList()
                : new List<IEvent>()))
            .ToList();
    }

    /// <summary>
    ///     Iterates a pre-snapshotted list of (aggregate, events) pairs
    ///     and dispatches each event through the in-process
    ///     <see cref="IMediator"/> bus. Handler exceptions are caught and
    ///     logged at <c>Error</c> per CC-9 (best-effort); the loop
    ///     continues to the next event so a single failing consumer
    ///     cannot starve the other consumers. After the loop, the
    ///     <c>ClearDomainEvents()</c> method is invoked on every concrete
    ///     aggregate that exposes it (today: <see cref="Alert"/>).
    /// </summary>
    /// <param name="pending">
    ///     The snapshot built by <see cref="SnapshotPendingEvents"/>.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token from the originating
    ///     <c>SavedChangesAsync</c> call. Forwarded to
    ///     <see cref="IMediator.PublishAsync"/> so a cancelled save also
    ///     cancels the in-flight dispatch.
    /// </param>
    private async Task DispatchAndClearAsync(
        List<(IHasDomainEvents Aggregate, List<IEvent> Events)> pending,
        CancellationToken cancellationToken)
    {
        foreach (var (aggregate, events) in pending)
        {
            foreach (var evt in events)
            {
                try
                {
                    await _mediator.PublishAsync(evt, cancellationToken);
                }
                catch (Exception ex)
                {
                    // CC-9: log + swallow. The DB write is already
                    // committed; rolling back would couple the domain
                    // transaction to consumer availability. The failed
                    // event is effectively lost (CC-2: in-process bus,
                    // no DLQ).
                    _logger.LogError(
                        ex,
                        "Failed to dispatch domain event {EventType} on aggregate {AggregateType}",
                        evt.GetType().Name,
                        aggregate.GetType().Name);
                }
            }

            // Clear only when the concrete aggregate exposes
            // ClearDomainEvents(). The IHasDomainEvents contract is
            // read-only; the clear method is a public member of the
            // concrete aggregate (Alert in Phase 2; future aggregates
            // extend this type check as they adopt the contract).
            if (aggregate is Alert alert)
            {
                alert.ClearDomainEvents();
            }
        }
    }
}
