using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Entities;
using RescueRanger.Infrastructure.Data;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for validating user-tenant associations
/// </summary>
public class TenantUserValidationService : ITenantUserValidationService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantUserValidationService> _logger;
    
    public TenantUserValidationService(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        ITenantService tenantService,
        ILogger<TenantUserValidationService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _tenantService = tenantService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<bool> ValidateUserBelongsToTenantAsync(Guid userId, Guid tenantId)
    {
        try
        {
            return await _context.AllTenants<User>()
                .AnyAsync(u => u.Id == userId && u.TenantId == tenantId && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user {UserId} belongs to tenant {TenantId}", userId, tenantId);
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> ValidateEmailUniqueInTenantAsync(string email, Guid tenantId, Guid? excludeUserId = null)
    {
        try
        {
            var query = _context.AllTenants<User>()
                .Where(u => u.TenantId == tenantId && u.Email.ToLower() == email.ToLower());
                
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }
            
            return !await query.AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email uniqueness for {Email} in tenant {TenantId}", email, tenantId);
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<bool>> ValidateRoleAssignmentAsync(Guid userId, string role, Guid assignedByUserId)
    {
        try
        {
            // Get both users
            var targetUser = await _context.AllTenants<User>()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            var assigningUser = await _context.AllTenants<User>()
                .FirstOrDefaultAsync(u => u.Id == assignedByUserId && u.IsActive);
                
            if (targetUser is null)
                return Result<bool>.NotFound("Target user not found");
            if (assigningUser is null)
                return Result<bool>.NotFound("Assigning user not found");
                
            // Both users must be in the same tenant
            if (targetUser.TenantId != assigningUser.TenantId)
                return Result<bool>.Forbidden("Users must be in the same tenant");
                
            // Only admins can assign roles
            if (!IsUserAdmin(assigningUser) && !IsSystemAdmin(assigningUser))
                return Result<bool>.Forbidden("Only administrators can assign roles");
                
            // Validate role is allowed
            if (!IsValidRole(role))
                return Result<bool>.Invalid(new List<ValidationError> { new("Role", $"Invalid role: {role}") });
                
            // System admins cannot be demoted by tenant admins
            if (IsSystemAdmin(targetUser) && !IsSystemAdmin(assigningUser))
                return Result<bool>.Forbidden("Cannot modify system administrator roles");
                
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating role assignment for user {UserId} to role {Role}", userId, role);
            return Result<bool>.Error("Role assignment validation failed");
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<bool>> ValidateUserInvitationAsync(string email, Guid tenantId, Guid invitedByUserId)
    {
        try
        {
            // Check if inviting user exists and has permission
            var invitingUser = await _context.AllTenants<User>()
                .FirstOrDefaultAsync(u => u.Id == invitedByUserId && u.TenantId == tenantId && u.IsActive);
                
            if (invitingUser is null)
                return Result<bool>.NotFound("Inviting user not found");
                
            // Only managers and admins can invite
            if (!CanUserInviteOthers(invitingUser))
                return Result<bool>.Forbidden("User does not have permission to invite others");
                
            // Check if email already exists in tenant
            var emailExists = await _context.AllTenants<User>()
                .AnyAsync(u => u.TenantId == tenantId && u.Email.ToLower() == email.ToLower());
                
            if (emailExists)
                return Result<bool>.Invalid(new List<ValidationError> { new("Email", $"User with email {email} already exists in this organization") });
                
            // Validate tenant exists and is active
            var tenant = await _tenantService.GetTenantInfoAsync(tenantId);
            if (tenant is null)
                return Result<bool>.NotFound("Tenant not found");
                
            // TODO: Check tenant user limits if applicable
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user invitation for {Email} to tenant {TenantId}", email, tenantId);
            return Result<bool>.Error("User invitation validation failed");
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<bool>> ValidateUserRemovalAsync(Guid userId, Guid removedByUserId)
    {
        try
        {
            var targetUser = await _context.AllTenants<User>()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            var removingUser = await _context.AllTenants<User>()
                .FirstOrDefaultAsync(u => u.Id == removedByUserId && u.IsActive);
                
            if (targetUser is null)
                return Result<bool>.NotFound("Target user not found");
            if (removingUser is null)
                return Result<bool>.NotFound("Removing user not found");
                
            // Users must be in the same tenant
            if (targetUser.TenantId != removingUser.TenantId)
                return Result<bool>.Forbidden("Users must be in the same tenant");
                
            // Users cannot remove themselves
            if (userId == removedByUserId)
                return Result<bool>.Forbidden("Cannot remove yourself");
                
            // Only admins can remove users
            if (!IsUserAdmin(removingUser) && !IsSystemAdmin(removingUser))
                return Result<bool>.Forbidden("Only administrators can remove users");
                
            // System admins cannot be removed by tenant admins
            if (IsSystemAdmin(targetUser) && !IsSystemAdmin(removingUser))
                return Result<bool>.Forbidden("Cannot remove system administrators");
                
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user removal for user {UserId}", userId);
            return Result<bool>.Error("User removal validation failed");
        }
    }
    
    /// <inheritdoc />
    public async Task<UserTenantValidationContext?> GetUserTenantValidationContextAsync(Guid userId)
    {
        try
        {
            var user = await _context.AllTenants<User>()
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                
            if (user is null)
                return null;
                
            var tenant = await _tenantService.GetTenantInfoAsync(user.TenantId);
            if (tenant is null)
                return null;
                
            var isAdmin = IsUserAdmin(user);
            var canManageUsers = CanUserManageOthers(user);
            var canAssignRoles = isAdmin || IsSystemAdmin(user);
            
            return new UserTenantValidationContext
            {
                User = user,
                Tenant = tenant,
                IsActiveInTenant = user.IsActive,
                Role = user.Role,
                IsTenantAdmin = isAdmin,
                CanManageUsers = canManageUsers,
                CanAssignRoles = canAssignRoles
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user tenant validation context for user {UserId}", userId);
            return null;
        }
    }
    
    private static bool IsUserAdmin(User user)
    {
        return user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
               user.Role.Equals("TenantAdmin", StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool IsSystemAdmin(User user)
    {
        return user.Role.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase) ||
               user.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool CanUserInviteOthers(User user)
    {
        return IsUserAdmin(user) || IsSystemAdmin(user) ||
               user.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool CanUserManageOthers(User user)
    {
        return IsUserAdmin(user) || IsSystemAdmin(user) ||
               user.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool IsValidRole(string role)
    {
        var validRoles = new[] { "Volunteer", "Manager", "Admin", "TenantAdmin" };
        return validRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}