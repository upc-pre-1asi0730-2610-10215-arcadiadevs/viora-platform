using System.Text.Json;

using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Entity type configuration for the Plot aggregate root.
/// </summary>
public class PlotConfiguration : IEntityTypeConfiguration<Plot>
{
    /// <summary>
    ///     Configures the Plot entity type.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Plot> builder)
    {
        builder.ToTable("plots");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(p => p.PlotName)
            .HasColumnName("plot_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(p => p.AreaSize)
            .HasColumnName("area_size")
            .HasColumnType("decimal(18,6)")
            .IsRequired();

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(p => p.AgroMonitoringPolygonId)
            .HasColumnName("agro_monitoring_polygon_id")
            .HasMaxLength(64);

        builder.Property(p => p.AgroMonitoringCenter)
            .HasColumnName("agro_monitoring_center")
            .HasMaxLength(64);

        // Configure PolygonCoordinates as owned entity type with JSON storage
        builder.OwnsOne(p => p.PolygonCoordinates, polygonBuilder =>
        {
            polygonBuilder.Property(p => p.Points)
                .HasColumnName("polygon_coordinates")
                .HasColumnType("jsonb")
                .HasConversion(
                    new ValueConverter<IReadOnlyList<GeoPoint>, string>(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<GeoPoint>>(v, (JsonSerializerOptions?)null) ?? new List<GeoPoint>()),
                    new ValueComparer<IReadOnlyList<GeoPoint>>(
                        (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions?)null),
                        c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null).GetHashCode(),
                        c => c));
        });

        builder.OwnsOne(p => p.ChillRequirementOverride, chillBuilder =>
        {
            chillBuilder.OwnsOne(c => c.Portions, pBuilder =>
            {
                pBuilder.Property(p => p.Value)
                    .HasColumnName("chill_requirement_portions")
                    .HasColumnType("decimal(18,6)");
            });

            chillBuilder.Property(c => c.Source)
                .HasColumnName("chill_requirement_source")
                .HasConversion<string>()
                .HasMaxLength(50);

            chillBuilder.Property(c => c.Model)
                .HasColumnName("chill_requirement_model")
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
