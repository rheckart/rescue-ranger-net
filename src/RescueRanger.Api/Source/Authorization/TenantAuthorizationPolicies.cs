using Microsoft.AspNetCore.Authorization;

namespace RescueRanger.Api.Authorization;

/// <summary>
/// Constants for tenant authorization policies
/// </summary>
public static class TenantAuthorizationPolicies
{
    // Basic tenant policies
    public const string TenantUser = "TenantUser";
    public const string TenantAdmin = "TenantAdmin";
    public const string TenantManager = "TenantManager";
    
    // System-wide policies
    public const string SystemAdmin = "SystemAdmin";
    public const string CrossTenantAccess = "CrossTenantAccess";
    
    // User management policies
    public const string UserManagement = "UserManagement";
    public const string UserInvitation = "UserInvitation";
    public const string RoleAssignment = "RoleAssignment";
    
    // Resource-based policies
    public const string HorseManagement = "HorseManagement";
    public const string VolunteerManagement = "VolunteerManagement";
    public const string ReportAccess = "ReportAccess";
}

/// <summary>
/// Extension methods for configuring tenant authorization policies
/// </summary>
public static class TenantAuthorizationExtensions
{
    /// <summary>
    /// Adds tenant-aware authorization policies
    /// </summary>
    public static IServiceCollection AddTenantAuthorization(this IServiceCollection services)
    {
        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, TenantAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, CrossTenantAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, UserManagementAuthorizationHandler>();
        
        services.AddAuthorization(options =>
        {
            // Basic tenant user policy - must belong to current tenant
            options.AddPolicy(TenantAuthorizationPolicies.TenantUser, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true));
            });
            
            // Tenant admin policy - must be admin in current tenant
            options.AddPolicy(TenantAuthorizationPolicies.TenantAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true,
                    requireTenantAdmin: true));
            });
            
            // Tenant manager policy - managers and admins
            options.AddPolicy(TenantAuthorizationPolicies.TenantManager, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    requiredRole: "Manager",
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true));
            });
            
            // System admin policy - global administrators only
            options.AddPolicy(TenantAuthorizationPolicies.SystemAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new CrossTenantAuthorizationRequirement("SystemAdmin"));
            });
            
            // Cross-tenant access policy - system admins with tenant switching
            options.AddPolicy(TenantAuthorizationPolicies.CrossTenantAccess, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new CrossTenantAuthorizationRequirement(
                    "CrossTenantAccess", 
                    allowTenantSwitching: true));
            });
            
            // User management policy - admins and managers
            options.AddPolicy(TenantAuthorizationPolicies.UserManagement, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new UserManagementAuthorizationRequirement(
                    UserManagementOperation.ManageUser));
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    requiredRole: "Manager",
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true));
            });
            
            // User invitation policy - admins and managers can invite
            options.AddPolicy(TenantAuthorizationPolicies.UserInvitation, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new UserManagementAuthorizationRequirement(
                    UserManagementOperation.InviteUser));
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    requiredRole: "Manager",
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true));
            });
            
            // Role assignment policy - only admins can assign roles
            options.AddPolicy(TenantAuthorizationPolicies.RoleAssignment, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new UserManagementAuthorizationRequirement(
                    UserManagementOperation.AssignRole));
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    requiredRole: "Admin",
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true));
            });
            
            // Horse management policy - volunteers and above
            options.AddPolicy(TenantAuthorizationPolicies.HorseManagement, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    resource: "Horse",
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true));
            });
            
            // Volunteer management policy - managers and admins
            options.AddPolicy(TenantAuthorizationPolicies.VolunteerManagement, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    requiredRole: "Manager",
                    resource: "Volunteer",
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true));
            });
            
            // Report access policy - all authenticated tenant users
            options.AddPolicy(TenantAuthorizationPolicies.ReportAccess, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new TenantAuthorizationRequirement(
                    resource: "Report",
                    requireTenantMembership: true,
                    allowSystemAdminBypass: true));
            });
        });
        
        return services;
    }
}

/// <summary>
/// Attributes for easy policy application on endpoints
/// </summary>
public class TenantUserAttribute : AuthorizeAttribute
{
    public TenantUserAttribute() : base(TenantAuthorizationPolicies.TenantUser) { }
}

public class TenantAdminAttribute : AuthorizeAttribute  
{
    public TenantAdminAttribute() : base(TenantAuthorizationPolicies.TenantAdmin) { }
}

public class TenantManagerAttribute : AuthorizeAttribute
{
    public TenantManagerAttribute() : base(TenantAuthorizationPolicies.TenantManager) { }
}

public class SystemAdminAttribute : AuthorizeAttribute
{
    public SystemAdminAttribute() : base(TenantAuthorizationPolicies.SystemAdmin) { }
}

public class CrossTenantAccessAttribute : AuthorizeAttribute
{
    public CrossTenantAccessAttribute() : base(TenantAuthorizationPolicies.CrossTenantAccess) { }
}

public class UserManagementAttribute : AuthorizeAttribute
{
    public UserManagementAttribute() : base(TenantAuthorizationPolicies.UserManagement) { }
}

public class UserInvitationAttribute : AuthorizeAttribute
{
    public UserInvitationAttribute() : base(TenantAuthorizationPolicies.UserInvitation) { }
}

public class RoleAssignmentAttribute : AuthorizeAttribute
{
    public RoleAssignmentAttribute() : base(TenantAuthorizationPolicies.RoleAssignment) { }
}