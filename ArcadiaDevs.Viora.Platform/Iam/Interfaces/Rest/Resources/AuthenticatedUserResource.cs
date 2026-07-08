namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

public record AuthenticatedUserResource(int Id, string Username, string Token, string Role);
