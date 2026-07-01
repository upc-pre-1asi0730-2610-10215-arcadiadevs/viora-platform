using ArcadiaDevs.Viora.Platform.Shared.Domain;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Deterministic <see cref="IClock"/> implementation for tests. The
///     returned <see cref="UtcNow"/> value is a settable, lock-protected
///     field that tests can advance via <see cref="Advance"/>,
///     overwrite via <see cref="Set"/>, or branch via
///     <see cref="With"/>. Thread-safe so concurrent readers and
///     writers in parallel test scenarios do not race.
/// </summary>
/// <remarks>
///     <para>
///         Default ctor seeds the clock to <c>2026-06-30T00:00:00Z</c>,
///         a future date relative to most test fixtures, so that
///         token-expiration tests have a stable baseline. The seed
///         ctor is provided for tests that need an explicit anchor
///         (e.g. for a known <c>DateTime</c>).
///     </para>
///     <para>
///         Replaces the 28+ call sites that previously used
///         <c>Substitute.For&lt;IClock&gt;()</c>; per the Phase 3
///         design §1.3 this fake is the canonical test double for
///         the time abstraction.
///     </para>
/// </remarks>
public sealed class FakeClock : IClock
{
    private readonly object _lock = new();
    private DateTime _utcNow;

    public FakeClock()
        : this(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc))
    {
    }

    public FakeClock(DateTime seed)
    {
        if (seed.Kind != DateTimeKind.Utc)
        {
            seed = DateTime.SpecifyKind(seed, DateTimeKind.Utc);
        }
        _utcNow = seed;
    }

    /// <inheritdoc />
    public DateTime UtcNow
    {
        get
        {
            lock (_lock)
            {
                return _utcNow;
            }
        }
    }

    /// <summary>
    ///     Replaces the current time with <paramref name="newUtcNow"/>.
    ///     Any subsequent <see cref="UtcNow"/> read returns the new
    ///     value (under the same lock).
    /// </summary>
    public void Set(DateTime newUtcNow)
    {
        if (newUtcNow.Kind != DateTimeKind.Utc)
        {
            newUtcNow = DateTime.SpecifyKind(newUtcNow, DateTimeKind.Utc);
        }
        lock (_lock)
        {
            _utcNow = newUtcNow;
        }
    }

    /// <summary>
    ///     Advances the current time by <paramref name="delta"/>. Negative
    ///     deltas move the clock backwards (useful for "what if this
    ///     happened yesterday" scenarios).
    /// </summary>
    public void Advance(TimeSpan delta)
    {
        lock (_lock)
        {
            _utcNow = _utcNow.Add(delta);
        }
    }

    /// <summary>
    ///     Returns a NEW <see cref="FakeClock"/> that starts at
    ///     <paramref name="newUtcNow"/>. The original clock is left
    ///     unchanged — useful for branching a scenario into "what
    ///     happens at time T vs. time T+1h" without disturbing other
    ///     tests that share the original clock.
    /// </summary>
    public FakeClock With(DateTime newUtcNow) => new(newUtcNow);
}
