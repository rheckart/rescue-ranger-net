using RescueRanger.Core.Enums;
using RescueRanger.Core.ValueObjects;

namespace RescueRanger.Core.Models;

/// <summary>
/// Represents tenant information used in the application context
/// </summary>
public class TenantInfo
{
    /// <summary>
    /// Unique identifier for the tenant
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tenant name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;
    
    /// <summary>
    /// Tenant status
    /// </summary>
    public TenantStatus Status { get; set; }
    
    /// <summary>
    /// Tenant configuration
    /// </summary>
    public TenantConfiguration Configuration { get; set; } = new();
    
    /// <summary>
    /// Whether this tenant is active and can be accessed
    /// </summary>
    public bool IsActive => Status == TenantStatus.Active;
    
    /// <summary>
    /// Whether this tenant can be accessed (active or provisioning)
    /// </summary>
    public bool CanAccess => Status == TenantStatus.Active || Status == TenantStatus.Provisioning;
    
    /// <summary>
    /// Whether this is the system tenant
    /// </summary>
    public bool IsSystemTenant { get; set; }
    
    /// <summary>
    /// When the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the tenant was activated
    /// </summary>
    public DateTime? ActivatedAt { get; set; }
    
    /// <summary>
    /// When the tenant was suspended
    /// </summary>
    public DateTime? SuspendedAt { get; set; }
    
    /// <summary>
    /// Reason for suspension
    /// </summary>
    public string? SuspensionReason { get; set; }
    
    /// <summary>
    /// Creates an empty TenantInfo instance
    /// </summary>
    public static TenantInfo Empty => new()
    {
        Id = Guid.Empty,
        Name = string.Empty,
        Subdomain = string.Empty,
        Status = TenantStatus.Inactive
    };
    
    /// <summary>
    /// Validates the tenant info
    /// </summary>
    public bool IsValid => Id != Guid.Empty && !string.IsNullOrEmpty(Subdomain);
}