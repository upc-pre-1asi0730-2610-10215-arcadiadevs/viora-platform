namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;

public record UserSessionResource(int Id, string UserAgent, DateTime LastActiveAt, bool IsCurrent);
