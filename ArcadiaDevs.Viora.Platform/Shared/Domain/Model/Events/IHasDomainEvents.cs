namespace ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

/// <summary>
///     Marker interface for aggregates that raise domain events during state
///     transitions. Events are collected in <see cref="DomainEvents"/> and are
///     intended to be dispatched post-commit by a
///     <c>SaveChangesInterceptor</c> (CC-4 in the Phase 1 cross-cutting
///     conventions).
/// </summary>
/// <remarks>
///     <para>
///         Phase 1 only introduces the contract and the field collection on
///         the implementing aggregates. The actual post-commit dispatcher is
///         a later phase (SHARED-011, out of Phase 1 scope). Aggregates that
///         implement this interface expose their pending events as a
///         read-only collection; callers must not mutate the collection.
///     </para>
///     <para>
///         Events are typed as <see cref="IEvent"/> so the future dispatcher
///         can route them through the existing in-process
///         <c>Cortex.Mediator</c> bus without an additional layer of
///         abstraction.
///     </para>
/// </remarks>
public interface IHasDomainEvents
{
    /// <summary>
    ///     The collection of domain events raised since the aggregate was
    ///     loaded or last cleared. Read-only; the underlying storage is
    ///     implementation-specific.
    /// </summary>
    IReadOnlyCollection<IEvent> DomainEvents { get; }
}
