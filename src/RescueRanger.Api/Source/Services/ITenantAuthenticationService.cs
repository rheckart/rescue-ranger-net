using Ardalis.Result;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for tenant-aware user authentication
/// </summary>
public interface ITenantAuthenticationService
{
    /// <summary>
    /// Authenticates a user within the current tenant context
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <returns>Authentication result with user and tokens</returns>
    Task<Result<TenantAuthenticationResult>> AuthenticateAsync(string email, string password);
    
    /// <summary>
    /// Refreshes an authentication token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="ipAddress">Request IP address</param>
    /// <returns>New authentication result</returns>
    Task<Result<TenantAuthenticationResult>> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
    
    /// <summary>
    /// Revokes a refresh token
    /// </summary>
    /// <param name="refreshToken">Token to revoke</param>
    /// <param name="ipAddress">Request IP address</param>
    /// <returns>Success result</returns>
    Task<Result> RevokeTokenAsync(string refreshToken, string? ipAddress = null);
    
    /// <summary>
    /// Validates user access to current tenant
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if user has access</returns>
    Task<bool> ValidateUserTenantAccessAsync(Guid userId);
    
    /// <summary>
    /// Switches tenant for system admin users
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="targetTenantId">Target tenant ID</param>
    /// <returns>New authentication result for target tenant</returns>
    Task<Result<TenantAuthenticationResult>> SwitchTenantAsync(Guid userId, Guid targetTenantId);
}

/// <summary>
/// Result of tenant-aware authentication
/// </summary>
public class TenantAuthenticationResult
{
    /// <summary>
    /// Authenticated user
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Tenant information
    /// </summary>
    public TenantInfo Tenant { get; set; } = null!;
    
    /// <summary>
    /// JWT access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime TokenExpiresAt { get; set; }
    
    /// <summary>
    /// Whether this is a tenant switch operation
    /// </summary>
    public bool IsTenantSwitch { get; set; }
    
    /// <summary>
    /// Original tenant ID (for tenant switching)
    /// </summary>
    public Guid? OriginalTenantId { get; set; }
}