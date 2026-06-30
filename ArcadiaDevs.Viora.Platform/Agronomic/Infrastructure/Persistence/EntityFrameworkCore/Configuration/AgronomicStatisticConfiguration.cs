using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the <see cref="AgronomicStatistic"/> aggregate root.
/// </summary>
/// <remarks>
///     Maps the AgronomicStatistic aggregate to the <c>agronomic_statistics</c> table
///     in snake_case following the project convention. The <c>ChillModelState</c> value
///     object is flattened via <see cref="EntityTypeBuilder{TEntity}.OwnsOne" /> so the
///     three sub-fields land in dedicated columns on the same row.
/// </remarks>
public class AgronomicStatisticConfiguration : IEntityTypeConfiguration<AgronomicStatistic>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AgronomicStatistic> builder)
    {
        builder.ToTable("agronomic_statistics");

        // AGRO-002: read/write backing fields directly so the private setters
        // on the aggregate are not bypassed. Required for the factory-method +
        // private-setter hardening pattern.
        builder.UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(s => s.PlotId)
            .HasColumnName("plot_id")
            .IsRequired();

        builder.Property(s => s.MeasurementDate)
            .HasColumnName("measurement_date")
            .IsRequired();

        builder.Property(s => s.NdviValue)
            .HasColumnName("ndvi_value")
            .IsRequired();

        builder.Property(s => s.ChillPortions)
            .HasColumnName("chill_portions")
            .IsRequired();

        builder.Property(s => s.ChillHours)
            .HasColumnName("chill_hours")
            .IsRequired();

        builder.OwnsOne(s => s.ChillModelState, ms =>
        {
            ms.Property<long>("AgronomicStatisticId").HasColumnName("id");

            ms.Property(m => m.IntermediateProduct)
                .HasColumnName("chill_model_intermediate_product")
                .IsRequired();

            ms.Property(m => m.PreviousHourTemperatureCelsius)
                .HasColumnName("chill_model_previous_hour_temperature_celsius");

            ms.Property(m => m.PriorHourTemperatureCelsius)
                .HasColumnName("chill_model_prior_hour_temperature_celsius");
        });

        builder.HasIndex(s => new { s.PlotId, s.MeasurementDate })
            .HasDatabaseName("ix_agronomic_statistics_plot_id_measurement_date");

        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("ix_agronomic_statistics_user_id");
    }
}
