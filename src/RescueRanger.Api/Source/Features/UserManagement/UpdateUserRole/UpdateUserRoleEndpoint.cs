using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;
using RescueRanger.Infrastructure.Data;

namespace RescueRanger.Api.Features.UserManagement.UpdateUserRole;

/// <summary>
/// Request to update a user's role
/// </summary>
public class UpdateUserRoleRequest
{
    /// <summary>
    /// User ID to update
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// New role for the user
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Reason for role change (for audit)
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Response for role update
/// </summary>
public class UpdateUserRoleResponse
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Previous role
    /// </summary>
    public string PreviousRole { get; set; } = string.Empty;
    
    /// <summary>
    /// New role
    /// </summary>
    public string NewRole { get; set; } = string.Empty;
    
    /// <summary>
    /// When the change was made
    /// </summary>
    public DateTime UpdatedAt { get; set; }
    
    /// <summary>
    /// Who made the change
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint for updating user roles
/// </summary>
public class UpdateUserRoleEndpoint : Endpoint<UpdateUserRoleRequest, UpdateUserRoleResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantUserIdentityService _userIdentity;
    private readonly ITenantUserValidationService _validationService;
    private readonly ILogger<UpdateUserRoleEndpoint> _logger;
    
    public UpdateUserRoleEndpoint(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        ITenantUserIdentityService userIdentity,
        ITenantUserValidationService validationService,
        ILogger<UpdateUserRoleEndpoint> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _userIdentity = userIdentity;
        _validationService = validationService;
        _logger = logger;
    }
    
    public override void Configure()
    {
        Put("/users/{userId}/role");
        Policies(TenantAuthorizationPolicies.RoleAssignment);
        Summary(s => s
            .Summary("Update a user's role")
            .Description("Updates the role of a user within the current tenant")
            .Response(200, "Role updated successfully")
            .Response(400, "Invalid request data")
            .Response(403, "Insufficient permissions")
            .Response(404, "User not found"));
    }
    
    public override async Task HandleAsync(UpdateUserRoleRequest req, CancellationToken ct)
    {
        try
        {
            if (!_tenantContext.IsValid)
            {
                await Send.ForbidAsync(ct);
                return;
            }
            
            var currentUserId = _userIdentity.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
            
            // Validate role assignment
            var validationResult = await _validationService.ValidateRoleAssignmentAsync(
                req.UserId, req.Role, currentUserId.Value);
                
            if (!validationResult.IsSuccess)
            {
                await SendAsync(Results.BadRequest(validationResult.Errors));
                return;
            }
            
            // Get the target user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == req.UserId, ct);
                
            if (user is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            
            var previousRole = user.Role;
            
            // Update the role
            user.Role = req.Role;
            user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing tokens
            
            await _context.SaveChangesAsync(ct);
            
            var currentUser = await _userIdentity.GetCurrentUserAsync();
            var updatedBy = currentUser?.FullName ?? "System";
            
            var response = new UpdateUserRoleResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                PreviousRole = previousRole,
                NewRole = user.Role,
                UpdatedAt = user.UpdatedAt ?? DateTime.UtcNow,
                UpdatedBy = updatedBy
            };
            
            _logger.LogInformation("User {UserId} role changed from {PreviousRole} to {NewRole} by {UpdatedBy}",
                user.Id, previousRole, user.Role, updatedBy);
            
            await Send.OkAsync(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role for user {UserId}", req.UserId);
            await SendAsync(Results.Problem("An error occurred while updating the user's role"));
        }
    }
}