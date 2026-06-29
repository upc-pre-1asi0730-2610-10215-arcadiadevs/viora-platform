namespace ArcadiaDevs.Viora.Platform.Shared.Domain;

/// <summary>
///     Abstraction over the system clock so that time-sensitive application
///     services can be tested with a fixed substitute instead of pinning the
///     wall-clock at the call site (SHARED-008).
/// </summary>
/// <remarks>
///     The <see cref="UtcNow"/> property returns a <see cref="DateTime"/> whose
///     <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Utc"/>. Application
///     code MUST use this abstraction instead of <c>DateTime.UtcNow</c> /
///     <c>DateTimeOffset.UtcNow</c> directly so that tests can drive time
///     deterministically.
/// </remarks>
public interface IClock
{
    /// <summary>
    ///     Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}
