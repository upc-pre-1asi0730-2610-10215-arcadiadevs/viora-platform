namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     A single item of required personal protective equipment for an
///     <see cref="AgrochemicalPrescription" /> (REQ-TP-3). Matches OS's enum
///     literal names exactly.
/// </summary>
public enum PersonalProtectiveEquipment
{
    MASK,
    GLOVES,
    GOGGLES,
    COVERALLS
}
