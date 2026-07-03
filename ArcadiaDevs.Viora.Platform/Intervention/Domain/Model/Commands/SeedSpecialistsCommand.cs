namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;

/// <summary>
///     Command to trigger the seeding of demo Specialist rows (each backed
///     by a real Profile row with Role=Specialist) into the database.
///     Mirrors the Surveillance BC's <c>SeedSymptomsCommand</c> pattern
///     (design decision 1, obs #267).
/// </summary>
public record SeedSpecialistsCommand();
