using RescueRanger.Core.Models;
using RescueRanger.Core.ValueObjects;

namespace RescueRanger.Core.Services;

/// <summary>
/// Service for managing the current tenant context
/// </summary>
public interface ITenantContextService
{
    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    Guid TenantId { get; }
    
    /// <summary>
    /// Gets the current tenant subdomain
    /// </summary>
    string TenantSubdomain { get; }
    
    /// <summary>
    /// Gets the current tenant name
    /// </summary>
    string TenantName { get; }
    
    /// <summary>
    /// Gets the current tenant info
    /// </summary>
    TenantInfo? CurrentTenant { get; }
    
    /// <summary>
    /// Indicates if a valid tenant is set
    /// </summary>
    bool IsValid { get; }
    
    /// <summary>
    /// Sets the current tenant context
    /// </summary>
    void SetTenant(TenantInfo tenantInfo);
    
    /// <summary>
    /// Sets the current tenant context
    /// </summary>
    void SetTenant(Guid tenantId, string subdomain, string name);
    
    /// <summary>
    /// Clears the current tenant context
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Gets the tenant configuration asynchronously
    /// </summary>
    Task<TenantConfiguration?> GetTenantConfigurationAsync();
    
    /// <summary>
    /// Validates if the current tenant can be accessed
    /// </summary>
    Task<bool> ValidateTenantAccessAsync();
    
    /// <summary>
    /// Checks if the current tenant is the system tenant
    /// </summary>
    bool IsSystemTenant { get; }
}