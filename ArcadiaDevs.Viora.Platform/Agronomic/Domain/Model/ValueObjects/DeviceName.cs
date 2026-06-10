using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Value object representing the human-readable name of an IoT device.
/// </summary>
public readonly record struct DeviceName
{
    public string Value { get; init; }
    
    public DeviceName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("DeviceName must not be blank", nameof(value));
        }

        if (value.Length > 150)
        {
            throw new ArgumentException("DeviceName must not exceed 150 characters", nameof(value));
        }

        Value = value;
    }
}