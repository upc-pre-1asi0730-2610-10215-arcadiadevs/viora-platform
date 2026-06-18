using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for active nutrition plan per plot.
/// </summary>
/// <remarks>
///     Returned by GET /api/v1/plots/{plotId}/dynamic-nutrition/active-plan.
/// </remarks>
public record DynamicNutritionPlanResource
{
    /// <summary>Identifier of the plot.</summary>
    public int PlotId { get; init; }

    /// <summary>Display name of the plot.</summary>
    public string PlotName { get; init; } = string.Empty;

    /// <summary>Placeholder plan identifier.</summary>
    public int PlanId { get; init; }

    /// <summary>Placeholder plan name.</summary>
    public string PlanName { get; init; } = string.Empty;

    /// <summary>Status: "active", "pending", or "completed".</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Plan start date.</summary>
    public DateTimeOffset StartDate { get; init; }

    /// <summary>Plan end date.</summary>
    public DateTimeOffset EndDate { get; init; }

    /// <summary>Nutrient details.</summary>
    public IReadOnlyList<NutrientResource> Nutrients { get; init; } = Array.Empty<NutrientResource>();
}
