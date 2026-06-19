using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public static class ENutritionPlanStatusExtensions
{
    public static ENutritionPlanStatus FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("NutritionPlanStatus is required");
        }

        if (Enum.TryParse<ENutritionPlanStatus>(value, true, out var status))
        {
            return status;
        }

        throw new ArgumentException($"Invalid NutritionPlanStatus '{value}'. Allowed: ACTIVE, SUPERSEDED, EXPIRED, COMPLETED");
    }
}
