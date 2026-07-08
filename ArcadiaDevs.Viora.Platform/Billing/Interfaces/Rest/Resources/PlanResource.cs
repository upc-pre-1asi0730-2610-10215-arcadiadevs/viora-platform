namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

public record PlanResource(
    int Id,
    string Code,
    string Name,
    decimal PriceAmount,
    string Currency,
    string Interval,
    string Tagline,
    IReadOnlyList<string> Features,
    int PlotLimit,
    int IotLimit);