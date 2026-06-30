using System.Text.RegularExpressions;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Value object for an IoT device activation (claim) code.
///     <para>
///         A producer claims a physical sensor by entering the code printed on its label.
///         The code's shape is <c>VIORA-&lt;TT&gt;&lt;NN&gt;-&lt;XXXX&gt;</c> where <c>TT</c> encodes the
///         sensor kind (<c>SP</c> = soil probe, <c>LW</c> = leaf wetness, <c>WS</c> = weather station).
///     </para>
///     <para>
///         This VO guarantees the FORMAT; whether the code corresponds to a real issued unit
///         is a separate check against the <c>IActivationCodeCatalog</c>.
///     </para>
/// </summary>
public sealed record ActivationCode
{
    /// <summary>
    ///     Compiled regex enforcing the VIORA-TTNN-XXXX shape.
    ///     The prefix is anchored; <c>TT</c> matches <c>SP|LW|WS</c>, <c>NN</c> is two digits,
    ///     and <c>XXXX</c> is four upper-case alphanumeric characters.
    /// </summary>
    private static readonly Regex Pattern = new(
        "^VIORA-(SP|LW|WS)\\d{2}-[A-Z0-9]{4}$",
        RegexOptions.Compiled);

    /// <summary>
    ///     The normalized activation code (trimmed and upper-cased).
    /// </summary>
    public string Value { get; }

    /// <summary>
    ///     Builds a new <see cref="ActivationCode"/> from a raw string.
    /// </summary>
    /// <param name="value">The raw code; will be trimmed and upper-cased before validation.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="value"/> is null, blank, or does not match the expected pattern.
    /// </exception>
    public ActivationCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Activation code is required", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (!Pattern.IsMatch(normalized))
        {
            throw new ArgumentException(
                $"Invalid activation code format '{normalized}'. Expected VIORA-<SP|LW|WS><NN>-<XXXX>.",
                nameof(value));
        }

        Value = normalized;
    }

    /// <summary>
    ///     Derives the sensor kind from the two-letter prefix that follows <c>VIORA-</c>.
    /// </summary>
    /// <returns>The <see cref="IoTDeviceType"/> encoded in the code's prefix.</returns>
    public IoTDeviceType DeviceType()
    {
        // Indices are safe: the regex enforces the literal "VIORA-" (6 chars) and a 2-char prefix.
        var prefix = Value.Substring(6, 2);
        return prefix switch
        {
            "SP" => IoTDeviceType.SoilProbe,
            "LW" => IoTDeviceType.LeafWetness,
            "WS" => IoTDeviceType.WeatherStation,
            _ => throw new InvalidOperationException(
                $"Unreachable: prefix '{prefix}' validated by the pattern at construction time.")
        };
    }
}
