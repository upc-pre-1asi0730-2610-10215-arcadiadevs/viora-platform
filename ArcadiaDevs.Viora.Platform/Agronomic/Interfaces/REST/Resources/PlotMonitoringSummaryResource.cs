using System;
using System.Collections.Generic;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record NdviTrendSeriesResource(
    DateTimeOffset Timestamp,
    double Mean,
    double Minimum,
    double Maximum,
    double Median
);

public record NdviTrendResource(
    string Direction,
    double ChangeRate,
    IEnumerable<NdviTrendSeriesResource> Series
);

public record WeatherSummaryResource(
    string WeatherStatus,
    string MeasurementDate,
    string ClimateRiskLevel,
    double Temperature
);

public record RecommendationResource(
    string ActionType,
    string NutritionInputRecommendation,
    string ApplicationWindowStart,
    string ApplicationWindowEnd
);

public record ExternalSourceResource(
    string Provider,
    string Availability,
    DateTimeOffset LastReadingAt,
    int UpdateFrequencyMinutes
);

public record PlotMonitoringSummaryResource(
    long PlotId,
    long UserId,
    string PlotName,
    double CurrentNdvi,
    NdviTrendResource NdviTrend,
    double ChillPortions,
    double ChillPortionsWeeklyDelta,
    double ChillRequirementPortions,
    string ChillRequirementSource,
    string ChillMetricModel,
    string ChillUnit,
    string HealthStatus,
    string PhenologicalRisk,
    double YieldForecastTonnes,
    WeatherSummaryResource Weather,
    string ClimateRiskLevel,
    DateTimeOffset LastUpdatedAt,
    IEnumerable<RecommendationResource> Recommendations,
    ExternalSourceResource ClimateSource,
    ExternalSourceResource NdviSource
);
