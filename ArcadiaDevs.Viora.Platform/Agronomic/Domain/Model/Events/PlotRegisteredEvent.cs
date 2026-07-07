using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;

/// <summary>
///     Domain event raised after a new <see cref="Aggregates.Plot"/> is
///     successfully persisted (parity with OS's <c>PlotRegisteredEvent</c>).
/// </summary>
/// <remarks>
///     Published directly from <c>PlotCommandService</c> post-commit rather
///     than via the <see cref="IHasDomainEvents"/>/<c>PostCommitDomainEventDispatcher</c>
///     collection pattern: <see cref="PlotId"/> is database-generated and
///     unknown until after <c>SaveChanges</c>, so constructing the event
///     inside <c>Plot.Create</c> would permanently capture <c>0</c>. Mirrors
///     the same post-save direct-publish idiom already used by
///     <c>AlertCommandService.Handle(CreateAlertCommand)</c> for
///     <c>AlertCreatedEvent</c>.
/// </remarks>
public record PlotRegisteredEvent(
    int PlotId,
    int OwnerUserId
) : IEvent;
