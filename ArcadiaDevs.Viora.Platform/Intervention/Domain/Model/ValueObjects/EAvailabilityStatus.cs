namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The availability status of a <see cref="Aggregates.Specialist" />, used
///     by <see cref="Services.SpecialistMatchingPolicy" /> as the primary
///     ranking key (parity with OS's <c>SpecialistMatchingPolicy</c>).
/// </summary>
public enum EAvailabilityStatus
{
    AVAILABLE_TODAY,
    AVAILABLE_TOMORROW,
    AVAILABLE_THIS_WEEK,
    UNAVAILABLE
}
