using ArcadiaDevs.Viora.Platform.Shared.Infrastructure;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Domain;

/// <summary>
/// Unit tests for <see cref="SystemClock"/>.
/// </summary>
/// <remarks>
/// The clock is the foundation of every time-sensitive service. Pinning the
/// behaviour here lets the service-layer tests substitute an
/// <see cref="ArcadiaDevs.Viora.Platform.Shared.Domain.IClock"/> mock without
/// duplicating the real-clock assertion.
/// </remarks>
public class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsCurrentUtcTime_CloseToDateTimeUtcNow()
    {
        // GIVEN a real SystemClock
        var sut = new SystemClock();

        // WHEN reading UtcNow
        var before = DateTime.UtcNow;
        var actual = sut.UtcNow;
        var after = DateTime.UtcNow;

        // THEN the value lies between the two wall-clock samples (real time, not zero/fixed)
        Assert.InRange(actual, before.AddSeconds(-1), after.AddSeconds(1));

        // AND the Kind is UTC (the abstraction is named UtcNow for a reason)
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }
}
