namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Resources;

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
    string? PhotoUrl,
    double? Latitude,
    double? Longitude,
    double? ServiceRadiusKm,
    string? ServiceTags,
    string? Availability,
    bool ShowProBadge);
