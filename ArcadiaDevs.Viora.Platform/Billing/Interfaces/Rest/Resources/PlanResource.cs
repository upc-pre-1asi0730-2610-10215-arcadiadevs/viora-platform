namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     Plan catalog resource (REQ-PLAN-2).
/// </summary>
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