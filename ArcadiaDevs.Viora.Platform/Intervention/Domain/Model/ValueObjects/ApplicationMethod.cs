namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     The application method for an <see cref="AgrochemicalPrescription" />
///     (REQ-TP-3). Matches OS's enum literal names exactly.
/// </summary>
public enum ApplicationMethod
{
    HYDRAULIC_SPRAYING,
    DRIP_IRRIGATION,
    FOLIAR,
    SOIL_DRENCH,
    OTHER
}
