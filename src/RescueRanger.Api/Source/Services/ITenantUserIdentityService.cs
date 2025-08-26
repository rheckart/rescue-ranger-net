using System.Security.Claims;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for managing tenant-aware user identity
/// </summary>
public interface ITenantUserIdentityService
{
    /// <summary>
    /// Gets the current user from the HTTP context
    /// </summary>
    /// <returns>Current user if authenticated and valid</returns>
    Task<User?> GetCurrentUserAsync();
    
    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    /// <returns>User ID if available</returns>
    Guid? GetCurrentUserId();
    
    /// <summary>
    /// Gets the current user email from claims
    /// </summary>
    /// <returns>User email if available</returns>
    string? GetCurrentUserEmail();
    
    /// <summary>
    /// Checks if the current user has the specified role
    /// </summary>
    /// <param name="role">Role to check</param>
    /// <returns>True if user has the role</returns>
    bool HasRole(string role);
    
    /// <summary>
    /// Checks if the current user is a system administrator
    /// </summary>
    /// <returns>True if user is system admin</returns>
    bool IsSystemAdmin();
    
    /// <summary>
    /// Checks if the current user can switch tenants
    /// </summary>
    /// <returns>True if user can switch tenants</returns>
    bool CanSwitchTenant();
    
    /// <summary>
    /// Checks if the current session is a result of tenant switching
    /// </summary>
    /// <returns>True if this is a switched tenant session</returns>
    bool IsTenantSwitched();
    
    /// <summary>
    /// Gets the original tenant ID for switched sessions
    /// </summary>
    /// <returns>Original tenant ID if available</returns>
    Guid? GetOriginalTenantId();
    
    /// <summary>
    /// Validates that the current user belongs to the current tenant
    /// </summary>
    /// <returns>True if user belongs to tenant</returns>
    Task<bool> ValidateUserTenantAccessAsync();
    
    /// <summary>
    /// Creates a tenant-aware claims principal
    /// </summary>
    /// <param name="user">User entity</param>
    /// <param name="tenant">Tenant information</param>
    /// <returns>Claims principal with tenant context</returns>
    ClaimsPrincipal CreateTenantAwarePrincipal(User user, TenantInfo tenant);
}

/// <summary>
/// Tenant-aware user context information
/// </summary>
public class TenantUserContext
{
    /// <summary>
    /// User information
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Current tenant information
    /// </summary>
    public TenantInfo Tenant { get; set; } = null!;
    
    /// <summary>
    /// Whether this is a switched tenant session
    /// </summary>
    public bool IsTenantSwitched { get; set; }
    
    /// <summary>
    /// Original tenant ID (for switched sessions)
    /// </summary>
    public Guid? OriginalTenantId { get; set; }
    
    /// <summary>
    /// Whether the user is a system administrator
    /// </summary>
    public bool IsSystemAdmin { get; set; }
    
    /// <summary>
    /// Whether the user can switch tenants
    /// </summary>
    public bool CanSwitchTenant { get; set; }
}