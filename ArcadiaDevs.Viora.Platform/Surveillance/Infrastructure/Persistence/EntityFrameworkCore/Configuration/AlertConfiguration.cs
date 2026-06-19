using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).IsRequired().ValueGeneratedOnAdd();
        
        builder.OwnsOne(a => a.PlotId, pi =>
        {
            pi.Property<long>("AlertId").HasColumnName("id");
            pi.Property(p => p.Value).HasColumnName("PlotId");
        });

        builder.Property(a => a.Type).HasConversion<string>();
        builder.Property(a => a.Severity).HasConversion<string>();

        var stringListComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IList<string>>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToList()
        );

        var dictionaryComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IDictionary<string, string>>(
            (c1, c2) => c1!.Count == c2!.Count && !c1.Except(c2).Any(),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
            c => c.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );

        builder.Property(a => a.Sources)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            )
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(a => a.DataProviders)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            )
            .Metadata.SetValueComparer(stringListComparer);

        builder.Property(a => a.SupportingData)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
            )
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.HasMany(a => a.Timeline)
            .WithOne()
            .HasForeignKey(t => t.AlertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
