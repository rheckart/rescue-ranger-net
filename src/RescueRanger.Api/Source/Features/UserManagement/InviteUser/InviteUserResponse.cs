namespace RescueRanger.Api.Features.UserManagement.InviteUser;

/// <summary>
/// Response for user invitation
/// </summary>
public class InviteUserResponse
{
    /// <summary>
    /// ID of the invited user
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Email of the invited user
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Full name of the invited user
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Role assigned to the user
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether invitation email was sent
    /// </summary>
    public bool InvitationEmailSent { get; set; }
    
    /// <summary>
    /// Temporary password for the user (will be null in production)
    /// </summary>
    public string? TemporaryPassword { get; set; }
    
    /// <summary>
    /// When the invitation was created
    /// </summary>
    public DateTime InvitedAt { get; set; }
    
    /// <summary>
    /// Who invited the user
    /// </summary>
    public string InvitedBy { get; set; } = string.Empty;
}