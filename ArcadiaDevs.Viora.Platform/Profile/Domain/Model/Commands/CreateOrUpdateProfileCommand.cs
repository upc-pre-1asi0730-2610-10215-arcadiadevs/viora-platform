using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Commands;

/// <summary>
///     Command for creating or partially updating a profile (PUT upsert semantics).
/// </summary>
public record CreateOrUpdateProfileCommand(
    int UserId,
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
