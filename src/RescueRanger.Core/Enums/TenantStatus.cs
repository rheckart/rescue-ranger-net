namespace RescueRanger.Core.Enums;

/// <summary>
/// Represents the current operational status of a tenant
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// Tenant is being set up and not yet ready for use
    /// </summary>
    Provisioning = 0,
    
    /// <summary>
    /// Tenant is active and operational
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Tenant is temporarily suspended (e.g., for non-payment)
    /// </summary>
    Suspended = 2,
    
    /// <summary>
    /// Tenant is inactive but data is preserved
    /// </summary>
    Inactive = 3,
    
    /// <summary>
    /// Tenant is marked for deletion
    /// </summary>
    PendingDeletion = 4
}