namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

/// <summary>
///     Gated specialist contact resource (REQ-SPEC-2).
/// </summary>
public record SpecialistContactResource(int Id, string Email, string? Phone, string? Whatsapp);
