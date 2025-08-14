using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RescueRanger.Core.Entities;

namespace RescueRanger.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("Users");
        
        // Primary key
        builder.HasKey(u => u.Id);
        
        // Properties
        builder.Property(u => u.TenantId)
            .IsRequired();
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);
        
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(u => u.SecurityStamp)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Volunteer");
        
        builder.Property(u => u.ProfilePictureUrl)
            .HasMaxLength(500);
        
        builder.Property(u => u.PreferencesJson)
            .HasColumnType("jsonb");
        
        // Owned entity for RefreshTokens
        builder.OwnsMany(u => u.RefreshTokens, token =>
        {
            token.ToTable("UserRefreshTokens");
            token.WithOwner().HasForeignKey("UserId");
            token.Property(t => t.Token).HasMaxLength(500);
            token.Property(t => t.CreatedByIp).HasMaxLength(45);
            token.Property(t => t.RevokedByIp).HasMaxLength(45);
            token.Property(t => t.ReplacedByToken).HasMaxLength(500);
            token.HasKey("UserId", "Token");
        });
        
        // Audit fields
        builder.Property(u => u.CreatedAt)
            .IsRequired();
        
        builder.Property(u => u.CreatedBy)
            .IsRequired()
            .HasMaxLength(255);
        
        builder.Property(u => u.UpdatedAt);
        
        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(255);
        
        // Optimistic concurrency
        builder.Property(u => u.RowVersion)
            .IsRowVersion();
        
        // Indexes
        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique()
            .HasDatabaseName("IX_Users_TenantId_Email");
        
        builder.HasIndex(u => new { u.TenantId, u.IsActive })
            .HasDatabaseName("IX_Users_TenantId_IsActive");
        
        builder.HasIndex(u => u.SecurityStamp)
            .HasDatabaseName("IX_Users_SecurityStamp");
        
        // Seed data will be added via a separate migration after the schema is created
    }
}