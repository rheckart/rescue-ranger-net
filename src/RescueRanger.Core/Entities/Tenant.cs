using RescueRanger.Core.Enums;
using RescueRanger.Core.ValueObjects;

namespace RescueRanger.Core.Entities;

/// <summary>
/// Represents a tenant (organization) in the multi-tenant system
/// </summary>
public class Tenant : BaseEntity
{
    /// <summary>
    /// Name of the organization
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique subdomain for the tenant (e.g., "rescue1" in rescue1.rescueranger.com)
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary contact email for the organization
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Phone number for the organization
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Physical address of the organization
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Current status of the tenant
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;
    
    /// <summary>
    /// When the tenant was activated
    /// </summary>
    public DateTime? ActivatedAt { get; set; }
    
    /// <summary>
    /// When the tenant was suspended (if applicable)
    /// </summary>
    public DateTime? SuspendedAt { get; set; }
    
    /// <summary>
    /// Reason for suspension (if applicable)
    /// </summary>
    public string? SuspensionReason { get; set; }
    
    /// <summary>
    /// Configuration settings for this tenant
    /// </summary>
    public TenantConfiguration Configuration { get; set; } = new();
    
    /// <summary>
    /// Database connection string override (for future isolated database scenarios)
    /// </summary>
    public string? DatabaseConnectionString { get; set; }
    
    /// <summary>
    /// Storage connection string override (for tenant-specific storage)
    /// </summary>
    public string? StorageConnectionString { get; set; }
    
    /// <summary>
    /// API key for external integrations
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// When the API key was last rotated
    /// </summary>
    public DateTime? ApiKeyRotatedAt { get; set; }
    
    /// <summary>
    /// Indicates if this is the system admin tenant
    /// </summary>
    public bool IsSystemTenant { get; set; } = false;
    
    // Navigation properties (to be configured in EF)
    
    /// <summary>
    /// Validates if the tenant is in an active state
    /// </summary>
    public bool IsActive() => Status == TenantStatus.Active;
    
    /// <summary>
    /// Validates if the tenant can be accessed
    /// </summary>
    public bool CanAccess() => Status == TenantStatus.Active || Status == TenantStatus.Provisioning;
    
    /// <summary>
    /// Activates the tenant
    /// </summary>
    public void Activate()
    {
        Status = TenantStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        SuspendedAt = null;
        SuspensionReason = null;
    }
    
    /// <summary>
    /// Suspends the tenant
    /// </summary>
    public void Suspend(string reason)
    {
        Status = TenantStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
        SuspensionReason = reason;
    }
    
    /// <summary>
    /// Generates a new API key
    /// </summary>
    public void RotateApiKey()
    {
        ApiKey = GenerateApiKey();
        ApiKeyRotatedAt = DateTime.UtcNow;
    }
    
    private static string GenerateApiKey()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }
}