using Microsoft.AspNetCore.Authorization;

namespace RescueRanger.Api.Authorization;

/// <summary>
/// Authorization requirement for tenant-based access control
/// </summary>
public class TenantAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Required role for access
    /// </summary>
    public string? RequiredRole { get; }
    
    /// <summary>
    /// Whether the user must belong to the current tenant
    /// </summary>
    public bool RequireTenantMembership { get; }
    
    /// <summary>
    /// Whether system administrators can bypass tenant restrictions
    /// </summary>
    public bool AllowSystemAdminBypass { get; }
    
    /// <summary>
    /// Whether the operation requires tenant admin privileges
    /// </summary>
    public bool RequireTenantAdmin { get; }
    
    /// <summary>
    /// Resource being accessed (for resource-based authorization)
    /// </summary>
    public string? Resource { get; }
    
    public TenantAuthorizationRequirement(
        string? requiredRole = null,
        bool requireTenantMembership = true,
        bool allowSystemAdminBypass = true,
        bool requireTenantAdmin = false,
        string? resource = null)
    {
        RequiredRole = requiredRole;
        RequireTenantMembership = requireTenantMembership;
        AllowSystemAdminBypass = allowSystemAdminBypass;
        RequireTenantAdmin = requireTenantAdmin;
        Resource = resource;
    }
}

/// <summary>
/// Authorization requirement for cross-tenant operations (system admin only)
/// </summary>
public class CrossTenantAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Operation being performed across tenants
    /// </summary>
    public string Operation { get; }
    
    /// <summary>
    /// Whether tenant switching is allowed for this operation
    /// </summary>
    public bool AllowTenantSwitching { get; }
    
    public CrossTenantAuthorizationRequirement(string operation, bool allowTenantSwitching = false)
    {
        Operation = operation;
        AllowTenantSwitching = allowTenantSwitching;
    }
}

/// <summary>
/// Authorization requirement for user invitation and management
/// </summary>
public class UserManagementAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Type of user management operation
    /// </summary>
    public UserManagementOperation Operation { get; }
    
    /// <summary>
    /// Target user ID (for operations on specific users)
    /// </summary>
    public Guid? TargetUserId { get; }
    
    public UserManagementAuthorizationRequirement(UserManagementOperation operation, Guid? targetUserId = null)
    {
        Operation = operation;
        TargetUserId = targetUserId;
    }
}

/// <summary>
/// Types of user management operations
/// </summary>
public enum UserManagementOperation
{
    /// <summary>
    /// Inviting new users to the tenant
    /// </summary>
    InviteUser,
    
    /// <summary>
    /// Managing existing users (update, deactivate, etc.)
    /// </summary>
    ManageUser,
    
    /// <summary>
    /// Viewing user information
    /// </summary>
    ViewUser,
    
    /// <summary>
    /// Assigning roles to users
    /// </summary>
    AssignRole,
    
    /// <summary>
    /// Removing users from the tenant
    /// </summary>
    RemoveUser
}