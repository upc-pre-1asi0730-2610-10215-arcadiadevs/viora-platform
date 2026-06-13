using System.Linq;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Assembler to convert from DTO to Resource.
/// </summary>
public static class MonitoringSummaryResourceFromDtoAssembler
{
    public static MonitoringSummaryResource ToResourceFromDto(MonitoringSummaryDto dto)
    {
        return new MonitoringSummaryResource
        {
            MonitoringSummaryId = dto.MonitoringSummaryId,
            UserId = dto.UserId,
            GeneralHealthStatus = dto.GeneralHealthStatus,
            NdviValue = dto.NdviValue,
            AccumulatedChillHours = dto.AccumulatedChillHours,
            YieldForecast = dto.YieldForecast,
            MeasurementDate = dto.MeasurementDate,
            WeatherSnapshot = new WeatherSnapshotResource
            {
                WeatherStatus = dto.WeatherSnapshot.WeatherStatus,
                MeasurementDate = dto.WeatherSnapshot.MeasurementDate,
                ClimateRiskLevel = dto.WeatherSnapshot.ClimateRiskLevel,
                Temperature = dto.WeatherSnapshot.Temperature
            },
            ClimateRiskLevel = dto.ClimateRiskLevel,
            MitigationRecommendations = dto.MitigationRecommendations.Select(r => new MitigationRecommendationResource
            {
                ActionType = r.ActionType,
                NutritionInputRecommendation = r.NutritionInputRecommendation,
                ApplicationWindowStart = r.ApplicationWindowStart,
                ApplicationWindowEnd = r.ApplicationWindowEnd
            }).ToList()
        };
    }
}