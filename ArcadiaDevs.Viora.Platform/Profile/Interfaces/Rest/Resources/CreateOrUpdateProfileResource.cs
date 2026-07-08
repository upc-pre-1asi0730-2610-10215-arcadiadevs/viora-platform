using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Profile.Interfaces.Rest.Resources;

public record CreateOrUpdateProfileResource(
    string? FullName,
    string? Email,
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
    ESpecialistAvailability? Availability,
    bool? ShowProBadge);
