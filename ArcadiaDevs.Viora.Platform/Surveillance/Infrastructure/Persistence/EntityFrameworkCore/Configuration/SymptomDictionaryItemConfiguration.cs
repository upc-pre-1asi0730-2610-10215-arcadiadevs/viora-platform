using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

public class SymptomDictionaryItemConfiguration : IEntityTypeConfiguration<SymptomDictionaryItem>
{
    public void Configure(EntityTypeBuilder<SymptomDictionaryItem> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).IsRequired().ValueGeneratedNever();
    }
}
