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
    /// Indicates if a valid tenant is set
    /// </summary>
    bool IsValid { get; }
    
    /// <summary>
    /// Sets the current tenant context
    /// </summary>
    void SetTenant(Guid tenantId, string subdomain, string name);
    
    /// <summary>
    /// Clears the current tenant context
    /// </summary>
    void Clear();
}