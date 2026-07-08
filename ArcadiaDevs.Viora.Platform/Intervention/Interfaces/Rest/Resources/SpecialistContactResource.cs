namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;

public record SpecialistContactResource(int Id, string Email, string? Phone, string? Whatsapp, string Role, string? PhotoUrl);
