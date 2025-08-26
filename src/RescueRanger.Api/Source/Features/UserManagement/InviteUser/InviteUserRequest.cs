using System.ComponentModel.DataAnnotations;

namespace RescueRanger.Api.Features.UserManagement.InviteUser;

/// <summary>
/// Request to invite a new user to the current tenant
/// </summary>
public class InviteUserRequest
{
    /// <summary>
    /// Email address of the user to invite
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// First name of the user
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last name of the user
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Role to assign to the user
    /// </summary>
    [Required]
    public string Role { get; set; } = "Volunteer";
    
    /// <summary>
    /// Optional phone number
    /// </summary>
    [Phone]
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Whether to send an invitation email
    /// </summary>
    public bool SendInvitationEmail { get; set; } = true;
    
    /// <summary>
    /// Optional message to include in invitation
    /// </summary>
    [StringLength(500)]
    public string? InvitationMessage { get; set; }
}