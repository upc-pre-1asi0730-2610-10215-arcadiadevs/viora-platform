using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public record NdviValue
{
    public double Value { get; }

    protected NdviValue() { }

    public NdviValue(double value)
    {
        if (value < -1.0 || value > 1.0)
            throw new ArgumentException("NDVI value must be between -1.0 and 1.0.");
        
        Value = value;
    }
}
