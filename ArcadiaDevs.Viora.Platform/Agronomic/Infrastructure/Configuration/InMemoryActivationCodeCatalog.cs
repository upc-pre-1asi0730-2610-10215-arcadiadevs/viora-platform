using System.Collections.Immutable;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Configuration;

/// <summary>
///     In-memory implementation of <see cref="IActivationCodeCatalog"/> seeded with the
///     activation codes issued for the Viora demo fleet. Each code maps (by its prefix)
///     to a sensor kind: <c>SP</c> = soil probe, <c>LW</c> = leaf wetness,
///     <c>WS</c> = weather station.
///     <para>
///         Kept as a small fixed whitelist while there is no manufacturer integration;
///         swap for a persisted/remote registry without touching the domain.
///     </para>
///     <para>
///         Thread-safe via an <see cref="ImmutableHashSet{T}"/> snapshot of the issued
///         codes. The set is initialized once at type-load time and is never mutated
///         after the field assignment, so concurrent reads from the DI singleton are
///         safe without locks.
///     </para>
/// </summary>
public sealed class InMemoryActivationCodeCatalog : IActivationCodeCatalog
{
    private static readonly ImmutableHashSet<string> IssuedCodes = ImmutableHashSet.Create(StringComparer.Ordinal,
        // Soil probes (soil moisture + soil temperature)
        "VIORA-SP01-7K3M",
        "VIORA-SP02-9P2X",
        "VIORA-SP03-4T8H",
        // Leaf wetness sensors (leaf humidity)
        "VIORA-LW01-5F1N",
        "VIORA-LW02-7K9R",
        "VIORA-LW03-2M6Y",
        // Weather stations (all metrics)
        "VIORA-WS01-3H8V",
        "VIORA-WS02-8C4Q",
        "VIORA-WS03-1Z7Y");

    /// <inheritdoc />
    public bool IsIssued(ActivationCode code)
    {
        return code is not null && IssuedCodes.Contains(code.Value);
    }
}
