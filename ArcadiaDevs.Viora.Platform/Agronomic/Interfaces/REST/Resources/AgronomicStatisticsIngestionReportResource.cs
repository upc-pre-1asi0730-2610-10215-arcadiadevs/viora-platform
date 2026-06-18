namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record AgronomicStatisticsIngestionReportResource(
    int Ingested,
    int Skipped
);
