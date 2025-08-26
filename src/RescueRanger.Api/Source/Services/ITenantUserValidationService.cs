using Ardalis.Result;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for validating user-tenant associations
/// </summary>
public interface ITenantUserValidationService
{
    /// <summary>
    /// Validates that a user belongs to a specific tenant
    /// </summary>
    /// <param name="userId">User ID to validate</param>
    /// <param name="tenantId">Tenant ID to validate against</param>
    /// <returns>True if user belongs to tenant</returns>
    Task<bool> ValidateUserBelongsToTenantAsync(Guid userId, Guid tenantId);
    
    /// <summary>
    /// Validates that a user email is unique within a tenant
    /// </summary>
    /// <param name="email">Email to validate</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="excludeUserId">User ID to exclude from check (for updates)</param>
    /// <returns>True if email is unique within tenant</returns>
    Task<bool> ValidateEmailUniqueInTenantAsync(string email, Guid tenantId, Guid? excludeUserId = null);
    
    /// <summary>
    /// Validates that a user can be assigned a specific role within their tenant
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role to validate</param>
    /// <param name="assignedByUserId">User ID of the person assigning the role</param>
    /// <returns>Validation result</returns>
    Task<Result<bool>> ValidateRoleAssignmentAsync(Guid userId, string role, Guid assignedByUserId);
    
    /// <summary>
    /// Validates that a user invitation is valid
    /// </summary>
    /// <param name="email">Email being invited</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="invitedByUserId">User ID of inviter</param>
    /// <returns>Validation result</returns>
    Task<Result<bool>> ValidateUserInvitationAsync(string email, Guid tenantId, Guid invitedByUserId);
    
    /// <summary>
    /// Validates that a user can be removed from a tenant
    /// </summary>
    /// <param name="userId">User to remove</param>
    /// <param name="removedByUserId">User performing removal</param>
    /// <returns>Validation result</returns>
    Task<Result<bool>> ValidateUserRemovalAsync(Guid userId, Guid removedByUserId);
    
    /// <summary>
    /// Gets user tenant validation context
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Validation context</returns>
    Task<UserTenantValidationContext?> GetUserTenantValidationContextAsync(Guid userId);
}

/// <summary>
/// User tenant validation context
/// </summary>
public class UserTenantValidationContext
{
    /// <summary>
    /// User information
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Tenant information
    /// </summary>
    public TenantInfo Tenant { get; set; } = null!;
    
    /// <summary>
    /// Whether user is active in tenant
    /// </summary>
    public bool IsActiveInTenant { get; set; }
    
    /// <summary>
    /// User's role in tenant
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether user is tenant admin
    /// </summary>
    public bool IsTenantAdmin { get; set; }
    
    /// <summary>
    /// Whether user can manage other users
    /// </summary>
    public bool CanManageUsers { get; set; }
    
    /// <summary>
    /// Whether user can assign roles
    /// </summary>
    public bool CanAssignRoles { get; set; }
}