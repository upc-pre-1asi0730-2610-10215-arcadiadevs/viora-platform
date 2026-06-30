using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the <see cref="MonitoringSummary"/> aggregate root.
/// </summary>
/// <remarks>
///     Maps the MonitoringSummary aggregate to the <c>monitoring_summaries</c> table
///     in snake_case. The struct value objects (UserId, AverageNdvi, AccumulatedChillHours,
///     YieldProjection, LastSynchronizationAt) are flattened into dedicated columns via
///     value converters. The <c>WeatherSnapshot</c> and <c>MitigationRecommendation</c>
///     record value objects are flattened via <see cref="EntityTypeBuilder{TEntity}.ComplexProperty" />.
/// </remarks>
public class MonitoringSummaryConfiguration : IEntityTypeConfiguration<MonitoringSummary>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MonitoringSummary> builder)
    {
        builder.ToTable("monitoring_summaries");

        builder.HasKey(s => s.MonitoringSummaryId);

        builder.Property(s => s.MonitoringSummaryId)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => new MonitoringSummaryId(v))
            .ValueGeneratedOnAdd();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        builder.Property(s => s.GeneralHealthStatus)
            .HasColumnName("general_health_status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.AverageNdvi)
            .HasColumnName("average_ndvi")
            .HasConversion(v => v.Value, v => new AverageNdvi(v))
            .HasColumnType("decimal(4,2)")
            .IsRequired();

        builder.Property(s => s.AccumulatedChillHours)
            .HasColumnName("accumulated_chill_hours")
            .HasConversion(v => v.Value, v => new AccumulatedChillHours(v))
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(s => s.YieldProjection)
            .HasColumnName("yield_projection")
            .HasConversion(v => v.Value, v => new YieldProjection(v))
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(s => s.LastSynchronizationAt)
            .HasColumnName("last_synchronization_at")
            .HasConversion(v => v.Value, v => new LastSynchronizationAt(v))
            .IsRequired();

        builder.ComplexProperty(s => s.WeatherSnapshot, ws =>
        {
            ws.Property(w => w.CurrentTemperature)
                .HasColumnName("weather_current_temperature")
                .HasColumnType("decimal(5,2)")
                .IsRequired();

            ws.Property(w => w.WeatherStatus)
                .HasColumnName("weather_status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            ws.Property(w => w.LastValidatedReadingAt)
                .HasColumnName("weather_last_validated_reading_at")
                .IsRequired();

            ws.Property(w => w.ClimateRiskLevel)
                .HasColumnName("weather_climate_risk_level")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.ComplexProperty(s => s.MitigationRecommendation, mr =>
        {
            mr.Property(m => m.ActionType)
                .HasColumnName("mitigation_action_type")
                .HasMaxLength(100);

            mr.Property(m => m.SuggestedInputs)
                .HasColumnName("mitigation_suggested_inputs")
                .HasMaxLength(500);

            mr.Property(m => m.RecommendedApplicationWindow)
                .HasColumnName("mitigation_recommended_application_window")
                .HasMaxLength(100);
        });

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("ix_monitoring_summaries_user_id");
    }
}
