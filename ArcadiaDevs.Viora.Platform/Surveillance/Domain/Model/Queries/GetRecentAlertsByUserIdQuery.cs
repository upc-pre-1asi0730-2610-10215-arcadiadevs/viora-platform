namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;

public record GetRecentAlertsByUserIdQuery(long UserId, int Limit = 3);
