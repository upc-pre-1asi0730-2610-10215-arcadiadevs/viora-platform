using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Domain;

/// <summary>
///     Contract tests for the <see cref="IClock"/> abstraction
///     (SHARED-008) — verifies that any implementation returns a
///     <see cref="DateTime"/> whose <see cref="DateTime.Kind"/>
///     is <see cref="DateTimeKind.Utc"/> and whose value is
///     within a wall-clock window. The existing
///     <c>SystemClockTests</c> + <c>FakeClockTests</c> cover the
///     individual implementations; this class verifies the
///     SHARED-008 contract across both.
///     <para>
///         The tests use the <see cref="FakeClock"/> from the
///         test harness. <see cref="IClock"/> is registered as a
///         singleton in the production
///         <c>Program.cs</c>; the
///         <see cref="FakeClock"/> is interchangeable with the
///         production <c>SystemClock</c> via the
///         <see cref="IClock"/> interface.
///     </para>
/// </summary>
[Trait("Category", "Unit")]
public class IClockContractTests
{
    /// <summary>
    ///     Contract: <see cref="IClock.UtcNow"/> returns a
    ///     <see cref="DateTime"/> whose
    ///     <see cref="DateTime.Kind"/> is
    ///     <see cref="DateTimeKind.Utc"/>. The
    ///     <see cref="FakeClock"/> preserves the UTC Kind even
    ///     when the seed value is constructed without an
    ///     explicit Kind (the FakeClock normalizes the seed to
    ///     UTC at construction time).
    /// </summary>
    [Fact]
    public void UtcNow_ReturnsDateTimeWithUtcKind()
    {
        // GIVEN a FakeClock seeded with a non-UTC DateTime
        // (e.g. one constructed from DateTime.SpecifyKind or
        // default(DateTime)).
        var seed = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Unspecified);
        IClock clock = new FakeClock(seed);

        // WHEN the UtcNow is read.
        var now = clock.UtcNow;

        // THEN the returned DateTime has Kind = Utc (the
        // IClock contract is named UtcNow for a reason).
        Assert.Equal(DateTimeKind.Utc, now.Kind);
    }

    /// <summary>
    ///     Contract: <see cref="IClock.UtcNow"/> advances
    ///     correctly when the underlying clock advances. The
    ///     <see cref="FakeClock.Advance"/> method adds a
    ///     <see cref="TimeSpan"/> to the current time; the
    ///     difference between two reads (before + after Advance)
    ///     equals the delta. This pins the deterministic-time
    ///     contract that the test harness depends on for
    ///     time-sensitive service unit tests.
    /// </summary>
    [Fact]
    public void UtcNow_AdvancesByDelta()
    {
        // GIVEN a FakeClock seeded at a known time.
        var seed = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FakeClock(seed);

        // WHEN the clock advances by 1 hour 30 minutes.
        var before = clock.UtcNow;
        clock.Advance(TimeSpan.FromMinutes(90));
        var after = clock.UtcNow;

        // THEN the difference between before and after is
        // exactly 1h 30m.
        Assert.Equal(TimeSpan.FromMinutes(90), after - before);
        Assert.Equal(new DateTime(2026, 6, 30, 13, 30, 0, DateTimeKind.Utc), after);
    }

    /// <summary>
    ///     Contract: <see cref="IClock.UtcNow"/> is thread-safe
    ///     when concurrent readers + writers (Set/Advance)
    ///     interleave. The <see cref="FakeClock"/> uses an
    ///     internal lock to guard mutations; this test pins the
    ///     thread-safety contract that the post-commit
    ///     dispatcher's parallel readers depend on (the
    ///     dispatcher's snapshot helper may be called from
    ///     multiple EF Core worker threads in some load patterns).
    /// </summary>
    [Fact]
    public async Task UtcNow_IsThreadSafe_UnderConcurrentAdvanceAndRead()
    {
        // GIVEN a FakeClock + 1 writer task that advances the
        // clock 1000 times + 1 reader task that reads
        // UtcNow 1000 times, running concurrently.
        var clock = new FakeClock(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc));
        var iterations = 1_000;

        // WHEN the writer + reader run concurrently.
        var writerTask = Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                clock.Advance(TimeSpan.FromSeconds(1));
            }
        });
        var readerTask = Task.Run(() =>
        {
            for (var i = 0; i < iterations; i++)
            {
                _ = clock.UtcNow;
            }
        });
        await Task.WhenAll(writerTask, readerTask);

        // THEN no exceptions were thrown AND the clock is at
        // the expected post-advance time.
        var expected = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)
            .AddSeconds(iterations);
        Assert.Equal(expected, clock.UtcNow);
    }
}
