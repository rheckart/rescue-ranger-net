namespace RescueRanger.Core.Entities;

/// <summary>
/// Represents a user within a tenant organization
/// </summary>
public class User : TenantEntity
{
    /// <summary>
    /// Email address (unique within tenant)
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Full name for display
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    /// <summary>
    /// Phone number
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Hashed password
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Security stamp for invalidating tokens
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Whether the user's email is confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; }
    
    /// <summary>
    /// Whether the user's phone is confirmed
    /// </summary>
    public bool PhoneNumberConfirmed { get; set; }
    
    /// <summary>
    /// Whether two-factor authentication is enabled
    /// </summary>
    public bool TwoFactorEnabled { get; set; }
    
    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int AccessFailedCount { get; set; }
    
    /// <summary>
    /// When the user is locked out until
    /// </summary>
    public DateTime? LockoutEnd { get; set; }
    
    /// <summary>
    /// Whether lockout is enabled for this user
    /// </summary>
    public bool LockoutEnabled { get; set; } = true;
    
    /// <summary>
    /// User's role within the organization
    /// </summary>
    public string Role { get; set; } = "Volunteer";
    
    /// <summary>
    /// Whether the user is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }
    
    /// <summary>
    /// User preferences as JSON
    /// </summary>
    public string? PreferencesJson { get; set; }
    
    /// <summary>
    /// Refresh tokens for this user
    /// </summary>
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}

/// <summary>
/// Represents a refresh token for a user
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// The token value
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Whether the token is still active
    /// </summary>
    public bool IsActive => DateTime.UtcNow < ExpiresAt;
    
    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// IP address that created the token
    /// </summary>
    public string? CreatedByIp { get; set; }
    
    /// <summary>
    /// When the token was revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }
    
    /// <summary>
    /// IP address that revoked the token
    /// </summary>
    public string? RevokedByIp { get; set; }
    
    /// <summary>
    /// Token that replaced this one
    /// </summary>
    public string? ReplacedByToken { get; set; }
}