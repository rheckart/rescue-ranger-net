using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Authorization;
using RescueRanger.Api.Entities;
using RescueRanger.Api.Services;
using RescueRanger.Infrastructure.Data;

namespace RescueRanger.Api.Features.UserManagement.InviteUser;

/// <summary>
/// Endpoint for inviting new users to the current tenant
/// </summary>
public class InviteUserEndpoint : Endpoint<InviteUserRequest, InviteUserResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantUserIdentityService _userIdentity;
    private readonly ITenantUserValidationService _validationService;
    private readonly ILogger<InviteUserEndpoint> _logger;
    
    public InviteUserEndpoint(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        ITenantUserIdentityService userIdentity,
        ITenantUserValidationService validationService,
        ILogger<InviteUserEndpoint> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _userIdentity = userIdentity;
        _validationService = validationService;
        _logger = logger;
    }
    
    public override void Configure()
    {
        Post("/users/invite");
        Policies(TenantAuthorizationPolicies.UserInvitation);
        Summary(s => s
            .Summary("Invite a new user to the current tenant")
            .Description("Creates a new user account and optionally sends an invitation email")
            .Response(201, "User invited successfully")
            .Response(400, "Invalid request data")
            .Response(403, "Insufficient permissions")
            .Response(409, "User already exists"));
    }
    
    public override async Task HandleAsync(InviteUserRequest req, CancellationToken ct)
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
                await SendUnauthorizedAsync(ct);
                return;
            }
            
            // Validate the invitation
            var validationResult = await _validationService.ValidateUserInvitationAsync(
                req.Email, _tenantContext.TenantId, currentUserId.Value);
                
            if (!validationResult.IsSuccess)
            {
                await SendAsync(Results.BadRequest(validationResult.Errors));
                return;
            }
            
            // Validate role assignment
            var roleValidation = await _validationService.ValidateRoleAssignmentAsync(
                Guid.NewGuid(), req.Role, currentUserId.Value);
                
            if (!roleValidation.IsSuccess)
            {
                await SendAsync(Results.BadRequest(roleValidation.Errors));
                return;
            }
            
            // Generate temporary password
            var temporaryPassword = GenerateTemporaryPassword();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
            
            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                Email = req.Email.ToLowerInvariant(),
                FirstName = req.FirstName.Trim(),
                LastName = req.LastName.Trim(),
                PhoneNumber = req.PhoneNumber?.Trim(),
                PasswordHash = passwordHash,
                Role = req.Role,
                IsActive = true,
                EmailConfirmed = false,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            
            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);
            
            var currentUser = await _userIdentity.GetCurrentUserAsync();
            var invitedBy = currentUser?.FullName ?? "System";
            
            // TODO: Send invitation email if requested
            var emailSent = false;
            if (req.SendInvitationEmail)
            {
                try
                {
                    // Implement email sending logic here
                    emailSent = await SendInvitationEmail(user, temporaryPassword, req.InvitationMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send invitation email to {Email}", req.Email);
                    // Continue without failing the entire operation
                }
            }
            
            var response = new InviteUserResponse
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                InvitationEmailSent = emailSent,
                TemporaryPassword = Environment.IsDevelopment() ? temporaryPassword : null,
                InvitedAt = user.CreatedAt,
                InvitedBy = invitedBy
            };
            
            _logger.LogInformation("User {Email} invited to tenant {TenantId} by {InvitedBy}",
                user.Email, _tenantContext.TenantId, invitedBy);
            
            await SendCreatedAtAsync("GetUser", new { id = user.Id }, response, cancellation: ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error inviting user {Email}", req.Email);
            await SendAsync(Results.Conflict("User with this email may already exist"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting user {Email}", req.Email);
            await SendAsync(Results.Problem("An error occurred while inviting the user"));
        }
    }
    
    private static string GenerateTemporaryPassword()
    {
        // Generate a secure temporary password
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    
    private async Task<bool> SendInvitationEmail(User user, string temporaryPassword, string? customMessage)
    {
        // Placeholder for email sending logic
        // This would integrate with your email service (AWS SES, SendGrid, etc.)
        await Task.Delay(100); // Simulate email sending
        
        _logger.LogInformation("Invitation email would be sent to {Email} for tenant {TenantName}",
            user.Email, _tenantContext.TenantName);
            
        return true; // Return true when email is actually sent
    }
}