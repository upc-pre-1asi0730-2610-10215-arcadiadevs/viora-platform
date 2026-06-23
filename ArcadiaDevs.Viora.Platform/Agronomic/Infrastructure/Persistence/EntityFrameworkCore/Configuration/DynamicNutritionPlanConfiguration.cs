using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using System.Text.Json;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class DynamicNutritionPlanConfiguration : IEntityTypeConfiguration<DynamicNutritionPlan>
{
    public void Configure(EntityTypeBuilder<DynamicNutritionPlan> builder)
    {
        builder.ToTable("dynamic_nutrition_plans");
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Property(p => p.PlotId).IsRequired();
        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
        builder.Property(p => p.GeneratedDate).IsRequired();

        builder.OwnsOne(p => p.ApplicationWindow, a =>
        {
            a.Property<int>("DynamicNutritionPlanId").HasColumnName("id");
            a.Property(aw => aw.StartDate).HasColumnName("ApplicationWindowStart").IsRequired();
            a.Property(aw => aw.EndDate).HasColumnName("ApplicationWindowEnd").IsRequired();
        });

        builder.OwnsOne(p => p.Rationale, r =>
        {
            r.Property<int>("DynamicNutritionPlanId").HasColumnName("id");
            r.Property(pr => pr.Summary).HasColumnName("RationaleSummary").IsRequired().HasMaxLength(500);
            r.Property(pr => pr.TriggeringRiskLevel).HasColumnName("TriggeringRiskLevel").HasConversion<string>().IsRequired().HasMaxLength(50);
            r.Property(pr => pr.NdviValue)
                .HasColumnName("NdviValue")
                .HasConversion(n => n.Value, v => new NdviValue(v))
                .IsRequired()
                .HasColumnType("decimal(4,2)");
            r.Property(pr => pr.TemperatureAnomaly).HasColumnName("TemperatureAnomaly").IsRequired().HasColumnType("decimal(5,2)");
        });

        builder.OwnsOne(p => p.Application, a =>
        {
            a.Property<int>("DynamicNutritionPlanId").HasColumnName("id"); // Fix table splitting key mismatch
            
            a.Property(ap => ap.ApplicationDate).HasColumnName("ApplicationDate");
            a.Property(ap => ap.ApplicationTime).HasColumnName("ApplicationTime");
            
            var comparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyCollection<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => (IReadOnlyCollection<string>)c.ToList().AsReadOnly());

            a.Property(ap => ap.AppliedInputs)
                .HasColumnName("AppliedInputs")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<IReadOnlyCollection<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>().AsReadOnly())
                .Metadata.SetValueComparer(comparer);
            
            a.Property(ap => ap.DoseConfirmation).HasColumnName("DoseConfirmation").HasConversion<string>().HasMaxLength(50);
            a.Property(ap => ap.FieldOperator).HasColumnName("FieldOperator").HasMaxLength(100);
            a.Property(ap => ap.FieldNotes).HasColumnName("FieldNotes").HasMaxLength(500);
        });

        builder.OwnsMany(p => p.InputRecommendations, n =>
        {
            n.ToTable("dynamic_nutrition_plan_inputs");
            n.Property<int>("Id").IsRequired().ValueGeneratedOnAdd();
            n.HasKey("Id");
            n.Property(r => r.Value).IsRequired().HasMaxLength(100);
            n.Property(r => r.Purpose).IsRequired().HasMaxLength(250);
            n.Property(r => r.Dosage).IsRequired().HasColumnType("decimal(10,2)");
            n.Property(r => r.DosageUnit).IsRequired().HasMaxLength(20);
            n.Property(r => r.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
        });
    }
}
