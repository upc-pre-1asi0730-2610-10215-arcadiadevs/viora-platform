namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Provenance of a plot's chill requirement, expressing how much the value can be trusted.
/// </summary>
public enum EChillRequirementSource
{
    NotConfigured,
    SystemDefault,
    UserDeclared,
    AgronomistValidated
}
