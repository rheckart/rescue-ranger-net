using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Authorization;

/// <summary>
/// Authorization handler for tenant-based access control
/// </summary>
public class TenantAuthorizationHandler : AuthorizationHandler<TenantAuthorizationRequirement>
{
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantUserIdentityService _userIdentity;
    private readonly ILogger<TenantAuthorizationHandler> _logger;
    
    public TenantAuthorizationHandler(
        ITenantContextService tenantContext,
        ITenantUserIdentityService userIdentity,
        ILogger<TenantAuthorizationHandler> logger)
    {
        _tenantContext = tenantContext;
        _userIdentity = userIdentity;
        _logger = logger;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAuthorizationRequirement requirement)
    {
        try
        {
            // Check if user is authenticated
            if (context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogDebug("User not authenticated for tenant authorization");
                context.Fail();
                return;
            }
            
            // Check if tenant context is established
            if (!_tenantContext.IsValid)
            {
                _logger.LogWarning("Tenant context not established for authorization");
                context.Fail();
                return;
            }
            
            // Check system admin bypass
            if (requirement.AllowSystemAdminBypass && _userIdentity.IsSystemAdmin())
            {
                _logger.LogDebug("System admin bypassing tenant authorization requirements");
                context.Succeed(requirement);
                return;
            }
            
            // Validate user belongs to current tenant
            if (requirement.RequireTenantMembership)
            {
                var hasAccess = await _userIdentity.ValidateUserTenantAccessAsync();
                if (!hasAccess)
                {
                    _logger.LogWarning("User does not belong to current tenant {TenantId}", _tenantContext.TenantId);
                    context.Fail();
                    return;
                }
            }
            
            // Check required role
            if (!string.IsNullOrEmpty(requirement.RequiredRole))
            {
                if (!_userIdentity.HasRole(requirement.RequiredRole))
                {
                    _logger.LogWarning("User does not have required role {RequiredRole}", requirement.RequiredRole);
                    context.Fail();
                    return;
                }
            }
            
            // Check tenant admin requirement
            if (requirement.RequireTenantAdmin)
            {
                if (!IsTenantAdmin(context.User))
                {
                    _logger.LogWarning("User is not a tenant administrator");
                    context.Fail();
                    return;
                }
            }
            
            // All checks passed
            _logger.LogDebug("Tenant authorization successful for user {Email} in tenant {TenantId}",
                _userIdentity.GetCurrentUserEmail(), _tenantContext.TenantId);
            context.Succeed(requirement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant authorization");
            context.Fail();
        }
    }
    
    private static bool IsTenantAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || 
               user.IsInRole("TenantAdmin") || 
               user.IsInRole("Manager");
    }
}

/// <summary>
/// Authorization handler for cross-tenant operations
/// </summary>
public class CrossTenantAuthorizationHandler : AuthorizationHandler<CrossTenantAuthorizationRequirement>
{
    private readonly ITenantUserIdentityService _userIdentity;
    private readonly ILogger<CrossTenantAuthorizationHandler> _logger;
    
    public CrossTenantAuthorizationHandler(
        ITenantUserIdentityService userIdentity,
        ILogger<CrossTenantAuthorizationHandler> logger)
    {
        _userIdentity = userIdentity;
        _logger = logger;
    }
    
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CrossTenantAuthorizationRequirement requirement)
    {
        try
        {
            // Only system administrators can perform cross-tenant operations
            if (!_userIdentity.IsSystemAdmin())
            {
                _logger.LogWarning("Non-system admin attempting cross-tenant operation {Operation}", requirement.Operation);
                context.Fail();
                return Task.CompletedTask;
            }
            
            // Check if tenant switching is allowed for this operation
            if (requirement.AllowTenantSwitching && !_userIdentity.CanSwitchTenant())
            {
                _logger.LogWarning("User cannot switch tenants for operation {Operation}", requirement.Operation);
                context.Fail();
                return Task.CompletedTask;
            }
            
            _logger.LogDebug("Cross-tenant authorization successful for operation {Operation}", requirement.Operation);
            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cross-tenant authorization for operation {Operation}", requirement.Operation);
            context.Fail();
            return Task.CompletedTask;
        }
    }
}

/// <summary>
/// Authorization handler for user management operations
/// </summary>
public class UserManagementAuthorizationHandler : AuthorizationHandler<UserManagementAuthorizationRequirement>
{
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantUserIdentityService _userIdentity;
    private readonly ILogger<UserManagementAuthorizationHandler> _logger;
    
    public UserManagementAuthorizationHandler(
        ITenantContextService tenantContext,
        ITenantUserIdentityService userIdentity,
        ILogger<UserManagementAuthorizationHandler> logger)
    {
        _tenantContext = tenantContext;
        _userIdentity = userIdentity;
        _logger = logger;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserManagementAuthorizationRequirement requirement)
    {
        try
        {
            // Check authentication and tenant context
            if (context.User.Identity?.IsAuthenticated != true || !_tenantContext.IsValid)
            {
                context.Fail();
                return;
            }
            
            var currentUserId = _userIdentity.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                context.Fail();
                return;
            }
            
            // System admins can perform any user management operation
            if (_userIdentity.IsSystemAdmin())
            {
                context.Succeed(requirement);
                return;
            }
            
            // Check permissions based on operation type
            var hasPermission = requirement.Operation switch
            {
                UserManagementOperation.ViewUser => await CanViewUser(currentUserId.Value, requirement.TargetUserId),
                UserManagementOperation.InviteUser => CanInviteUser(context.User),
                UserManagementOperation.ManageUser => CanManageUser(context.User, currentUserId.Value, requirement.TargetUserId),
                UserManagementOperation.AssignRole => CanAssignRole(context.User),
                UserManagementOperation.RemoveUser => CanRemoveUser(context.User, currentUserId.Value, requirement.TargetUserId),
                _ => false
            };
            
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("User {UserId} does not have permission for operation {Operation}",
                    currentUserId, requirement.Operation);
                context.Fail();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user management authorization");
            context.Fail();
        }
    }
    
    private async Task<bool> CanViewUser(Guid currentUserId, Guid? targetUserId)
    {
        // Users can view their own profile
        if (targetUserId == currentUserId)
            return true;
            
        // Validate current user has tenant access
        return await _userIdentity.ValidateUserTenantAccessAsync();
    }
    
    private static bool CanInviteUser(ClaimsPrincipal user)
    {
        // Admins and managers can invite users
        return user.IsInRole("Admin") || 
               user.IsInRole("TenantAdmin") || 
               user.IsInRole("Manager");
    }
    
    private static bool CanManageUser(ClaimsPrincipal user, Guid currentUserId, Guid? targetUserId)
    {
        // Users can manage their own profile (limited)
        if (targetUserId == currentUserId)
            return true;
            
        // Admins and managers can manage other users
        return user.IsInRole("Admin") || 
               user.IsInRole("TenantAdmin") || 
               user.IsInRole("Manager");
    }
    
    private static bool CanAssignRole(ClaimsPrincipal user)
    {
        // Only admins can assign roles
        return user.IsInRole("Admin") || user.IsInRole("TenantAdmin");
    }
    
    private static bool CanRemoveUser(ClaimsPrincipal user, Guid currentUserId, Guid? targetUserId)
    {
        // Users cannot remove themselves
        if (targetUserId == currentUserId)
            return false;
            
        // Only admins can remove users
        return user.IsInRole("Admin") || user.IsInRole("TenantAdmin");
    }
}