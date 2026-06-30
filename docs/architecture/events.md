# Domain Events Architecture

This document describes how the `wa-viora-platform` API surfaces and processes
in-process domain events. It is the architectural reference for SHARED-011
(post-commit dispatcher) and CC-2 (in-process bus constraint). The goal is to
make the runtime contract — what is delivered, when, and how failures are
handled — explicit so that future bounded contexts and consumers can plug in
without re-deriving the rules from source.

## Table of contents

1. [In-process bus constraint (CC-2)](#1-in-process-bus-constraint-cc-2)
2. [`IEvent` design vs `IDomainEvent`](#2-ievent-design-vs-idomainevent)
3. [Post-commit dispatch contract](#3-post-commit-dispatch-contract)
4. [Failure-handling semantics (CC-9)](#4-failure-handling-semantics-cc-9)

---

## 1. In-process bus constraint (CC-2)

The platform uses **`Cortex.Mediator`'s in-process notification pipeline** as
the only event bus. There is **no outbox**, **no cross-process delivery**, and
**no dead-letter queue**. The consequences of this design are baked into every
consumer:

- **Process restart loses in-flight events.** A domain event that has been
  raised on an aggregate but whose `SaveChanges` has not yet committed is
  lost on process termination. A domain event that has been dispatched to
  handlers but whose handler has not yet completed is also lost. CC-9
  (best-effort) explicitly accepts this loss; the alternative — a durable
  outbox — is out of Phase 2 scope and would couple the bus to the DB write
  protocol.
- **Same-process delivery only.** A `Cortex.Mediator` event published by
  the Agronomic BC is delivered to every `IEventHandler<TEvent>` registered
  in the **same** host process. There is no fan-out to other nodes, no
  cross-pod delivery, no message broker. This matches the
  `os-viora-platform` Spring `afterCommit` semantics; cross-stack parity is
  preserved at the cost of horizontal scalability.
- **No retry, no DLQ.** A handler that throws is logged and the event is
  effectively lost (see §4 for the exact contract). Consumers that need
  at-least-once delivery must implement their own retry / idempotency layer
  on top of the event payload, OR the platform must migrate to a durable
  broker (out of Phase 2 scope).
- **The bus is a singleton.** `IMediator` is registered as a singleton by
  `AddCortexMediator([typeof(Program)])` in `Program.cs`; all consumers and
  the `PostCommitDomainEventDispatcher` share the same instance per host.

> **OS counterpart note:** `os-viora-platform` uses Spring's
> `TransactionalEventListener(phase = AFTER_COMMIT)` to achieve the same
> post-commit semantics. The Spring `ApplicationEventPublisher` is the
> singleton bus; events are Java records implementing `ApplicationEvent`;
> consumers are annotated `@TransactionalEventListener(phase = AFTER_COMMIT)`.
> The C# port is 1:1 except for the language-specific event / listener
> contracts (see §2).

## 2. `IEvent` design vs `IDomainEvent`

The original Phase 1 design sketched an `IDomainEvent` marker interface
(parity with the OS's `DomainEvent` base class). Phase 1 PR-8a deviated to
`IEvent : INotification` to integrate with the **Cortex.Mediator** bus that
the rest of the application already uses for commands and queries. The
deviation is locked (per engram #38) and is the only contract every domain
event in the platform implements.

```csharp
namespace ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

/// <summary>
/// Marker interface for domain events that travel on the
/// in-process Cortex.Mediator bus. Mirrors
/// Cortex.Mediator.Notifications.INotification; consumers implement
/// IEventHandler&lt;TEvent&gt; (see §2.2) and are auto-registered
/// by AddCortexMediator([typeof(Program)]).
/// </summary>
public interface IEvent : INotification
{
}
```

### 2.1 Concrete event shape

Concrete events are `public record`s (C# 14 positional records) carrying
**primitive** identifiers only (CC-1 contract). The recipient BC is
responsible for wrapping each primitive id in its own BC-local value object
before passing the value to an aggregate or a service:

```csharp
public record AgronomicChillDeficitIntegrationEvent(
    long PlotId,                      // CC-1: primitive transport
    decimal CurrentChillAccumulation,
    decimal TargetChill,
    decimal TemperatureAnomaly,
    DateTimeOffset DetectedAt
) : IEvent;
```

The class XML doc on every event carries the canonical CC-1 sentence:

> "Primitive transport, recipient must wrap PlotId in its own BC-local VO."

Wrapping the primitive is the recipient's job. The Agronomic BC's own
`PlotId` value object is **not** the right type at the receiving call site;
the recipient must use its own `PlotId` (e.g.
`Surveillance.Domain.Model.ValueObjects.PlotId` for the Surveillance
handler).

### 2.2 Consumer shape

Consumers implement `IEventHandler<TEvent>`, which is a thin alias over
Cortex.Mediator's `INotificationHandler<TEvent>`:

```csharp
namespace ArcadiaDevs.Viora.Platform.Shared.Application.Internal.EventHandlers;

public interface IEventHandler<in TEvent> : INotificationHandler<TEvent>
    where TEvent : IEvent
{
}
```

A consumer is a plain class (or primary-ctor class) with a `Handle(TEvent,
CancellationToken)` method. The consumer is **auto-registered** by
`AddCortexMediator([typeof(Program)])` in `Program.cs`; the registration
walks the assembly containing `Program` and binds every
`IEventHandler<TEvent>` implementation to the bus. **Do not** add an
explicit `AddSingleton<IEventHandler<...>, ...>()` registration for a
consumer — the auto-registration already covers it, and a manual
registration would either be a no-op or cause a duplicate-binding warning.

### 2.3 Reference

- `Shared/Domain/Model/Events/IHasDomainEvents.cs` — the aggregate-side
  contract (read-only `DomainEvents` collection).
- `Shared/Domain/Model/Events/IEvent.cs` — the event marker interface.
- `Shared/Application/Internal/EventHandlers/IEventHandler.cs` — the
  consumer contract.
- `Surveillance/Domain/Model/Aggregates/Alert.cs` — the only Phase 2
  `IHasDomainEvents` implementer that raises events at runtime (the
  `AlertUpdatedEvent` is raised on every state-machine transition).
- `Agronomic/Domain/Model/Events/AgronomicChillDeficitIntegrationEvent.cs`
  — the A5 cross-BC event (Agronomic → Surveillance); the producer is
  deferred to a future `IHostedService` phase (see its in-file `// TODO`
  block).

## 3. Post-commit dispatch contract

`PostCommitDomainEventDispatcher` is a `SaveChangesInterceptor` that
enumerates `IHasDomainEvents` instances from
`DbContext.ChangeTracker.Entries()`, snapshots their pending event
collections, and dispatches each event through the in-process bus AFTER
`SaveChanges` (or `SaveChangesAsync`) commits. The contract is a strict
**snapshot → commit → dispatch** sequence, with the snapshot step happening
BEFORE the commit to avoid a collection-modified-during-enumeration race.

### 3.1 Snapshot phase

For every tracked entity whose state is anything other than
`EntityState.Detached` and whose runtime type implements
`IHasDomainEvents`, the dispatcher takes a thread-local snapshot of the
aggregate's `DomainEvents` collection. The snapshot is a list of
`(IHasDomainEvents aggregate, List<IEvent> events)` tuples. The
`events` list is a `ToList()` copy, so the dispatch loop never re-reads
the live `IReadOnlyCollection<IEvent>` property on the aggregate — which
is the right thing, because the live collection is mutated by the
ChangeTracker during the commit.

```csharp
var pending = eventData.Context.ChangeTracker
    .Entries()
    .Where(entry =>
        entry.State != EntityState.Detached &&
        entry.Entity is IHasDomainEvents)
    .Select(entry => (
        (IHasDomainEvents)entry.Entity,
        ((IHasDomainEvents)entry.Entity).DomainEvents.ToList()))
    .ToList();
```

A tracked entity in any of the four non-`Detached` states (`Added`,
`Modified`, `Unchanged`, `Deleted`) is eligible. `Unchanged` is included
defensively in case a future implementation accumulates events on a
loaded-but-not-yet-modified aggregate; the current `Alert` state machine
only ever raises events on a `Modified` aggregate (the state-transition
methods mutate the aggregate before `SaveChanges` runs).

### 3.2 Commit phase

The dispatcher calls `base.SavedChangesAsync(eventData, result,
cancellationToken)` FIRST. This ensures the DB write is durable before
any consumer is notified — which is the defining property of a
post-commit hook. A consumer never sees an event whose transaction was
rolled back.

The dispatcher also handles the sync overload: `SavedChanges(eventData,
result)` is overridden to perform the same snapshot-then-commit-then-
dispatch sequence on the caller's thread. The sync overload delegates to
the same async dispatch helper via `.GetAwaiter().GetResult()` to block
the caller rather than fire-and-forget on `Task.Run` (which would be a
sync-over-async antipattern per design OQ #1).

### 3.3 Dispatch phase

If and only if the commit succeeded (`result > 0`), the dispatcher
iterates the snapshot and dispatches each event through
`IMediator.PublishAsync(evt, cancellationToken)`. After the inner loop
finishes (success or failure per CC-9), the dispatcher calls the
concrete aggregate's `ClearDomainEvents()` to drop the events from the
aggregate so the next `SaveChanges` does not re-dispatch them.

The `IHasDomainEvents` contract stays read-only (the `DomainEvents`
getter is the only member). The clear method is a public member of the
concrete aggregate; today only `Alert` implements it. The dispatcher
calls it via a runtime type check:

```csharp
if (aggregate is Alert alert)
{
    alert.ClearDomainEvents();
}
```

Future aggregates that implement `IHasDomainEvents` will add their own
`ClearDomainEvents()` method and the dispatcher will extend the type
check (or — future improvement — a default interface method on
`IHasDomainEvents` will make the call polymorphic without a type check).

### 3.4 Registration order (locked)

The dispatcher is registered via `AddInterceptors(...)` in the
`AddDbContext<AppDbContext>` lambda in `Program.cs`, in this exact order:

```csharp
options.AddInterceptors(
    sp.GetRequiredService<AuditableEntityInterceptor>(),     // FIRST
    sp.GetRequiredService<PostCommitDomainEventDispatcher>() // LAST
);
```

`AuditableEntityInterceptor` runs FIRST so the audit timestamps
(`CreatedAt` / `UpdatedAt`) are stamped on the entity BEFORE the
post-commit dispatcher reads the entity into the event payload. If the
audit interceptor ran AFTER the dispatcher, the event payload would
carry a stale (pre-stamp) timestamp and observers would see an
inconsistent view of the aggregate's audit state.

### 3.5 What is NOT dispatched

- Entities in `EntityState.Detached`. The dispatcher skips detached
  entries because their `DomainEvents` (if any) are stale: an entity
  that was attached, modified, and then detached never had its events
  committed, so dispatching them would notify consumers of an
  unpersisted state change.
- Aggregates that do not implement `IHasDomainEvents`. The dispatcher
  enumerates `ChangeTracker.Entries()` and filters by the type check;
  non-event-aggregates (e.g. `Plot`, `PestSightingReport`) are silently
  skipped.
- Events on a save that returned `result == 0`. A no-op save (every
  tracked entry was unchanged at commit time) means nothing was
  persisted; the events stay on the aggregates for the next attempt.
- Events on a save that threw (e.g. `DbUpdateException`). The exception
  propagates and the snapshot is dropped; the events stay on the
  aggregates, ready to be re-snapshotted on the next `SaveChanges`
  attempt.

## 4. Failure-handling semantics (CC-9)

The dispatcher is **best-effort**. The contract is:

- **Handler exceptions are logged at `Error` and swallowed.** A
  `try { _mediator.PublishAsync(...) } catch (Exception ex) { _logger.LogError(...) }`
  wraps every individual dispatch. The loop continues to the next
  event so a single failing consumer cannot starve the other consumers
  for the same aggregate.
- **The DB write is NOT rolled back.** The originating `SaveChanges`
  has already committed by the time the dispatch loop runs; rolling
  back would couple the domain write to consumer availability, which
  inverts the dependency and makes the platform brittle. The locked
  decision is: **domain writes succeed independently of consumer
  availability.**
- **FIFO ordering is preserved per aggregate.** The snapshot is a
  `List<IEvent>` copy of the aggregate's `IReadOnlyCollection<IEvent>`
  property; the dispatch loop iterates in enumeration order. Across
  aggregates, the dispatcher processes them in the order they appeared
  in the `ChangeTracker.Entries()` enumeration. A future improvement
  could parallelize the per-aggregate dispatch via `Task.WhenAll`, but
  FIFO is the v1 contract.
- **Thread safety.** The `ChangeTracker` is per-`DbContext` (scoped
  lifetime) and is NOT thread-safe by EF Core's design; the dispatcher
  runs on the same caller's thread that called `SaveChanges`, so the
  snapshot is safe. The `IMediator` bus is a singleton and IS
  thread-safe (Cortex.Mediator publishes synchronously inside
  `PublishAsync`'s continuation).
- **Process restart loses in-flight events.** A crash between
  `base.SavedChangesAsync` returning and the dispatch loop completing
  loses the un-dispatched events. The snapshot is in-memory; there is
  no replay log. A future `IHostedService` producer (the deferred
  `ChillDeficitMonitor` from A5) would re-publish the missed event
  from the source data on next startup, but a generic replay
  mechanism is out of Phase 2 scope.

### 4.1 Why best-effort

The platform's design philosophy is that **a domain transaction's success
must not depend on the availability of any consumer**. A surveillance
alert handler being down MUST NOT prevent an agronomic state transition
from being persisted; a notification service being down MUST NOT roll
back a pest sighting report. The alternative — at-least-once delivery
with retries and a DLQ — would require a durable broker and a separate
reliability layer, which is out of Phase 2 scope and would couple the
bus to the DB write protocol. The C# port is 1:1 with the OS's
`os-viora-platform` Spring `afterCommit` semantics on this point.

### 4.2 Operational guidance

- **Idempotency is the consumer's responsibility.** A consumer that
  cannot tolerate an at-least-once delivery MUST be idempotent (use the
  event's id, or a business-key derived from the payload, as a
  dedup token). Cortex.Mediator does not provide a dedup layer.
- **Logs are the source of truth.** A handler that swallows an
  exception MUST log enough context (the event type, the aggregate
  type, the exception) for an operator to investigate. The
  `PostCommitDomainEventDispatcher`'s catch block logs
  `evt.GetType().Name` and `aggregate.GetType().Name`; consumer-side
  catch blocks should log the same.
- **Cross-BC propagation is one-way.** A consumer that itself raises a
  domain event (e.g. a handler that calls a command service which
  raises an `AlertUpdatedEvent`) will go through the same post-commit
  dispatch path on the next `SaveChanges` call. The chain is
  naturally acyclic as long as the consumer does not modify the
  originating aggregate.

---

## Related documents

- [CHANGELOG.md](../../CHANGELOG.md) — entry for version `1.14.0` (the
  Phase 2 last-bump that ships this dispatcher).
- [AGENTS.md](../../AGENTS.md) — repo-level commit / branch conventions.
- `os-viora-platform` Spring `afterCommit` listener reference (out of
  tree; equivalent Java implementation for cross-stack parity check).
