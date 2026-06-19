using System;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Events;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Events;

public record DynamicNutritionRecommendedEvent(
    long PlanId,
    long PlotId,
    long UserId,
    string TriggeringRiskLevel) : IEvent;
