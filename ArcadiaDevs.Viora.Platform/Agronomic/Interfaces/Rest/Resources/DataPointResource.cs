namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record DataPointResource
{
    public DateTimeOffset Timestamp { get; init; }

    public decimal Ndvi { get; init; }

    public decimal ColdPortion { get; init; }
}
