namespace ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;

/// <summary>
///     The profile role — a fixed-value enum (not a DB-backed entity).
/// </summary>
/// <remarks>
///     Placement under ValueObjects mirrors the WeatherStatus precedent
///     in the Agronomic BC, not Iam's Role (which is a seeded DB entity).
/// </remarks>
public enum ProfileRole
{
    Producer,
    Specialist
}
