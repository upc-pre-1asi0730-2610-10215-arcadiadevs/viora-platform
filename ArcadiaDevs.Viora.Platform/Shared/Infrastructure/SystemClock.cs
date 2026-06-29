namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure;

/// <summary>
///     Production implementation of <see cref="Shared.Domain.IClock"/> that
///     delegates to <see cref="DateTime.UtcNow"/> (SHARED-008).
/// </summary>
/// <remarks>
///     Registered as a singleton because the implementation is stateless and
///     side-effect free. Per-test <c>FakeClock</c> substitutes will be added when
///     the dedicated test harness lands in a later phase.
/// </remarks>
public sealed class SystemClock : Shared.Domain.IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
