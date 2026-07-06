namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Resources;

/// <summary>
///     Resource representing a user's profile.
/// </summary>
public record ProfileResource(
    int UserId,
    string Role,
    string FullName,
    string Email,
    string? Phone,
    string? JobTitle,
    string? Language,
    string? Location,
    string? SpecialtyArea,
    string? PhotoUrl);
