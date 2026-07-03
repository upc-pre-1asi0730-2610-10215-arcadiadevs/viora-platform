namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Resources;

/// <summary>
///     Resource for creating or updating a profile via PUT.
/// </summary>
/// <remarks>
///     Role is intentionally absent — it is immutable after creation and
///     cannot be set or changed through this endpoint.
/// </remarks>
public record CreateOrUpdateProfileResource(
    string? FullName,
    string? Email,
    string? Phone,
    string? JobTitle,
    string? Language,
    string? Location,
    string? SpecialtyArea);
