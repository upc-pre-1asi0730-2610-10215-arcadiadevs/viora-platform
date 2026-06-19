using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Value object representing a specific nutrient requirement in a plan.
/// </summary>
public record Nutrient(
    string Name,
    decimal RequiredAmount,
    decimal CurrentAmount,
    string Unit);
