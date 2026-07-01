using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Xunit;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared;

/// <summary>
///     Unit tests for <see cref="FakeClock"/> (the deterministic
///     <see cref="IClock"/> used in Phase 3 test harness). The fake
///     clock must be thread-safe (per design §1.3) and must support
///     the Set/Advance/With API for time manipulation in tests.
/// </summary>
[Trait("Category", "Unit")]
public class FakeClockTests
{
    [Fact]
    public void UtcNow_InitiallyReturnsConstructorValue()
    {
        // Arrange
        var seed = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FakeClock(seed);

        // Act
        var actual = clock.UtcNow;

        // Assert
        Assert.Equal(seed, actual);
    }

    [Fact]
    public void Set_ReplacesCurrentTime()
    {
        // Arrange
        var clock = new FakeClock(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newTime = new DateTime(2026, 7, 15, 8, 30, 0, DateTimeKind.Utc);

        // Act
        clock.Set(newTime);

        // Assert
        Assert.Equal(newTime, clock.UtcNow);
    }

    [Fact]
    public void Advance_AddsTimeSpan()
    {
        // Arrange
        var clock = new FakeClock(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));

        // Act
        clock.Advance(TimeSpan.FromHours(3));

        // Assert
        Assert.Equal(new DateTime(2026, 6, 30, 15, 0, 0, DateTimeKind.Utc), clock.UtcNow);
    }

    [Fact]
    public void With_ReturnsClockWithNewStartingTime_LeavingOriginalUnchanged()
    {
        // Arrange
        var original = new FakeClock(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        var branched = original.With(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // Act — advance the branched clock; original must not move.
        branched.Advance(TimeSpan.FromDays(1));

        // Assert
        Assert.Equal(new DateTime(2027, 1, 2, 0, 0, 0, DateTimeKind.Utc), branched.UtcNow);
        Assert.Equal(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc), original.UtcNow);
    }

    [Fact]
    public async Task UtcNow_IsThreadSafe_WhenConcurrentReadersAndWriters()
    {
        // Arrange — verify the lock-based implementation does not throw
        // under concurrent Set/Advance/UtcNow access. This test pins
        // the thread-safety contract required by design §1.3.
        var clock = new FakeClock(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        var iterations = 1000;

        // Act
        var writerTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                clock.Advance(TimeSpan.FromSeconds(1));
            }
        });
        var readerTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                _ = clock.UtcNow;
            }
        });
        await Task.WhenAll(writerTask, readerTask);

        // Assert — no exceptions, the clock is now at initial + iterations seconds.
        var expected = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc)
            .AddSeconds(iterations);
        Assert.Equal(expected, clock.UtcNow);
    }

    [Fact]
    public void ImplementsIClock_Contract()
    {
        // Arrange — compile-time guarantee that FakeClock is interchangeable
        // with the production SystemClock in DI registrations.
        IClock clock = new FakeClock(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc));

        // Assert
        Assert.IsAssignableFrom<IClock>(clock);
    }
}
