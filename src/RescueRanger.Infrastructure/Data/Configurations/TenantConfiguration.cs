using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RescueRanger.Core.Entities;
using RescueRanger.Core.Enums;

namespace RescueRanger.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Table name
        builder.ToTable("Tenants");
        
        // Primary key
        builder.HasKey(t => t.Id);
        
        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(t => t.Subdomain)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(t => t.ContactEmail)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(t => t.PhoneNumber)
            .HasMaxLength(20);
        
        builder.Property(t => t.Address)
            .HasMaxLength(500);
        
        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);
        
        builder.Property(t => t.SuspensionReason)
            .HasMaxLength(500);
        
        builder.Property(t => t.DatabaseConnectionString)
            .HasMaxLength(1000);
        
        builder.Property(t => t.StorageConnectionString)
            .HasMaxLength(1000);
        
        builder.Property(t => t.ApiKey)
            .HasMaxLength(100);
        
        // Owned entity for Configuration
        builder.OwnsOne(t => t.Configuration, config =>
        {
            config.Property(c => c.MaxUsers).HasDefaultValue(10);
            config.Property(c => c.MaxHorses).HasDefaultValue(100);
            config.Property(c => c.AdvancedFeaturesEnabled).HasDefaultValue(false);
            config.Property(c => c.StorageLimitMb).HasDefaultValue(1024);
            
            // Store feature flags and metadata as JSON
            config.Property(c => c.FeatureFlags)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, bool>())
                .HasColumnType("jsonb");
            
            config.Property(c => c.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
                .HasColumnType("jsonb");
            
            // Owned entity for Branding
            config.OwnsOne(c => c.Branding, branding =>
            {
                branding.Property(b => b.PrimaryColor)
                    .HasMaxLength(7)
                    .HasDefaultValue("#1976D2");
                
                branding.Property(b => b.SecondaryColor)
                    .HasMaxLength(7)
                    .HasDefaultValue("#424242");
                
                branding.Property(b => b.LogoUrl)
                    .HasMaxLength(500);
                
                branding.Property(b => b.FaviconUrl)
                    .HasMaxLength(500);
                
                branding.Property(b => b.CustomCss)
                    .HasMaxLength(10000);
            });
        });
        
        // Audit fields
        builder.Property(t => t.CreatedAt)
            .IsRequired();
        
        builder.Property(t => t.CreatedBy)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(t => t.UpdatedAt);
        
        builder.Property(t => t.UpdatedBy)
            .HasMaxLength(255);
        
        // Optimistic concurrency
        builder.Property(t => t.RowVersion)
            .IsRowVersion();
        
        // Indexes
        builder.HasIndex(t => t.Subdomain)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_Subdomain");
        
        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_Tenants_Status");
        
        builder.HasIndex(t => t.ContactEmail)
            .HasDatabaseName("IX_Tenants_ContactEmail");
        
        // Seed data will be added via a separate migration after the schema is created
    }
}