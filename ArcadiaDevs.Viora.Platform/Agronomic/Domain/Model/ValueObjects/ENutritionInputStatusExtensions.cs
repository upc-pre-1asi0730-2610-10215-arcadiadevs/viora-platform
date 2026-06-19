using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public static class ENutritionInputStatusExtensions
{
    public static ENutritionInputStatus FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("NutritionInputStatus is required");
        }

        if (Enum.TryParse<ENutritionInputStatus>(value, true, out var status))
        {
            return status;
        }

        throw new ArgumentException($"Invalid NutritionInputStatus '{value}'. Allowed: RECOMMENDED, OPTIONAL");
    }
}
