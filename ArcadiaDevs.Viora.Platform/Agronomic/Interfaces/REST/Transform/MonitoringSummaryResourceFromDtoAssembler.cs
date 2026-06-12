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
            TotalPlots = dto.TotalPlots,
            TotalDevices = dto.TotalDevices,
            ActiveDevices = dto.ActiveDevices,
            InactiveDevices = dto.InactiveDevices,
            MaintenanceDevices = dto.MaintenanceDevices,
            AveragePlotArea = dto.AveragePlotArea,
            DeviceHealthPercentage = dto.DeviceHealthPercentage,
            ColdAccumulationIndex = dto.ColdAccumulationIndex,
            YieldProjection = dto.YieldProjection,
            AverageNdvi = dto.AverageNdvi,
            GeneralHealthStatus = dto.GeneralHealthStatus,
            LastSynchronizationAt = dto.LastSynchronizationAt,
            CurrentTemperature = dto.CurrentTemperature,
            WeatherStatus = dto.WeatherStatus,
            LastValidatedReadingAt = dto.LastValidatedReadingAt,
            ClimateRiskLevel = dto.ClimateRiskLevel,
            MitigationActionType = dto.MitigationActionType,
            MitigationSuggestedInputs = dto.MitigationSuggestedInputs,
            MitigationRecommendedWindow = dto.MitigationRecommendedWindow
        };
    }
}
