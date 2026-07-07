namespace ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices.Dtos;

/// <summary>
///     Gated specialist contact info (REQ-SPEC-2) — only returned once the
///     referenced <c>InterventionRequest</c> is ACCEPTED and matches the
///     requested specialist.
/// </summary>
public record SpecialistContact(int Id, string Email, string? Phone, string? Whatsapp, string Role, string? PhotoUrl);
