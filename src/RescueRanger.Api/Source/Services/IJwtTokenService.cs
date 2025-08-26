using System.Security.Claims;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for generating and validating JWT tokens with tenant context
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for a user with tenant claims
    /// </summary>
    /// <param name="user">The user to generate token for</param>
    /// <param name="tenant">The tenant context</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(User user, TenantInfo tenant);
    
    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <param name="ipAddress">IP address of the request</param>
    /// <returns>Refresh token</returns>
    RefreshToken GenerateRefreshToken(string? ipAddress = null);
    
    /// <summary>
    /// Validates a JWT token and returns claims
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);
    
    /// <summary>
    /// Extracts tenant information from claims
    /// </summary>
    /// <param name="claims">Claims principal</param>
    /// <returns>Tenant information if present</returns>
    (Guid? TenantId, string? TenantName)? ExtractTenantFromClaims(ClaimsPrincipal claims);
    
    /// <summary>
    /// Generates a token for tenant switching (admin users)
    /// </summary>
    /// <param name="user">The admin user</param>
    /// <param name="originalTenant">Original tenant</param>
    /// <param name="targetTenant">Target tenant to switch to</param>
    /// <returns>JWT token for tenant switching</returns>
    string GenerateTenantSwitchToken(User user, TenantInfo originalTenant, TenantInfo targetTenant);
}