using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RescueRanger.Api.Entities;

namespace RescueRanger.Infrastructure.Data.Configurations;

public class HorseConfiguration : IEntityTypeConfiguration<Horse>
{
    public void Configure(EntityTypeBuilder<Horse> builder)
    {
        // Table name
        builder.ToTable("Horses");
        
        // Primary key
        builder.HasKey(h => h.Id);
        
        // Properties
        builder.Property(h => h.TenantId)
            .IsRequired();
        
        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(h => h.Breed)
            .HasMaxLength(100);
        
        builder.Property(h => h.Color)
            .HasMaxLength(100);
        
        builder.Property(h => h.Gender)
            .HasMaxLength(20);
        
        builder.Property(h => h.HeightHands)
            .HasPrecision(4, 1);
        
        builder.Property(h => h.WeightPounds)
            .HasPrecision(6, 1);
        
        builder.Property(h => h.MicrochipNumber)
            .HasMaxLength(50);
        
        builder.Property(h => h.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("In Care");
        
        builder.Property(h => h.ArrivalDate)
            .IsRequired();
        
        builder.Property(h => h.MedicalNotes)
            .HasMaxLength(5000);
        
        builder.Property(h => h.BehavioralNotes)
            .HasMaxLength(5000);
        
        builder.Property(h => h.SpecialNeeds)
            .HasMaxLength(2000);
        
        builder.Property(h => h.AdoptionFee)
            .HasPrecision(10, 2);
        
        builder.Property(h => h.CurrentLocation)
            .HasMaxLength(200);
        
        // Store photo URLs as JSON array
        builder.Property(h => h.PhotoUrls)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");
        
        // Audit fields
        builder.Property(h => h.CreatedAt)
            .IsRequired();
        
        builder.Property(h => h.CreatedBy)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(h => h.UpdatedAt);
        
        builder.Property(h => h.UpdatedBy)
            .HasMaxLength(255);
        
        // Optimistic concurrency
        builder.Property(h => h.RowVersion)
            .IsRowVersion();
        
        // Indexes
        builder.HasIndex(h => h.TenantId)
            .HasDatabaseName("IX_Horses_TenantId");
        
        builder.HasIndex(h => new { h.TenantId, h.Status })
            .HasDatabaseName("IX_Horses_TenantId_Status");
        
        builder.HasIndex(h => new { h.TenantId, h.IsAvailableForAdoption })
            .HasDatabaseName("IX_Horses_TenantId_Available");
        
        builder.HasIndex(h => h.MicrochipNumber)
            .HasDatabaseName("IX_Horses_MicrochipNumber");
        
        // Global query filter for multi-tenancy (to be applied in DbContext)
        // This will be configured in the DbContext OnModelCreating
    }
}