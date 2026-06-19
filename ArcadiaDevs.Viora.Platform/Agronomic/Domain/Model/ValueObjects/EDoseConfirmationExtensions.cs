using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public static class EDoseConfirmationExtensions
{
    public static EDoseConfirmation FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Dose confirmation is required.");

        var normalizedValue = value.Trim().Replace("-", "_").Replace("_", "");
        if (Enum.TryParse<EDoseConfirmation>(normalizedValue, true, out var result))
        {
            return result;
        }

        throw new ArgumentException($"Invalid dose confirmation '{value}'. Allowed: AS_RECOMMENDED, ADJUSTED.");
    }
}
