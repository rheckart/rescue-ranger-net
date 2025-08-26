using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for generating and validating JWT tokens with tenant context
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly SymmetricSecurityKey _signingKey;
    
    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var signingKeyString = _configuration["Auth:SigningKey"] ?? 
                              throw new InvalidOperationException("Auth:SigningKey not configured");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKeyString));
    }
    
    /// <inheritdoc />
    public string GenerateToken(User user, TenantInfo tenant)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role),
            new("tenant_id", tenant.Id.ToString()),
            new("tenant_name", tenant.Name),
            new("tenant_subdomain", tenant.Subdomain),
            new("security_stamp", user.SecurityStamp),
            new("jti", Guid.NewGuid().ToString())
        };
        
        // Add additional claims for system admin users
        if (IsSystemAdmin(user))
        {
            claims.Add(new Claim("system_admin", "true"));
            claims.Add(new Claim("can_switch_tenant", "true"));
        }
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue("Auth:TokenExpiryMinutes", 60)),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
            Issuer = _configuration["Auth:Issuer"] ?? "RescueRanger",
            Audience = _configuration["Auth:Audience"] ?? "RescueRanger"
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }
    
    /// <inheritdoc />
    public RefreshToken GenerateRefreshToken(string? ipAddress = null)
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[64];
        rng.GetBytes(randomBytes);
        
        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(
                _configuration.GetValue("Auth:RefreshTokenExpiryDays", 7)),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }
    
    /// <inheritdoc />
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // Ensure it's a JWT token
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }
    
    /// <inheritdoc />
    public (Guid? TenantId, string? TenantName)? ExtractTenantFromClaims(ClaimsPrincipal claims)
    {
        var tenantIdClaim = claims.FindFirst("tenant_id");
        var tenantNameClaim = claims.FindFirst("tenant_name");
        
        if (tenantIdClaim is null || tenantNameClaim is null)
            return null;
            
        if (!Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            return null;
            
        return (tenantId, tenantNameClaim.Value);
    }
    
    /// <inheritdoc />
    public string GenerateTenantSwitchToken(User user, TenantInfo originalTenant, TenantInfo targetTenant)
    {
        if (!IsSystemAdmin(user))
            throw new UnauthorizedAccessException("Only system administrators can switch tenants");
            
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role),
            new("tenant_id", targetTenant.Id.ToString()),
            new("tenant_name", targetTenant.Name),
            new("tenant_subdomain", targetTenant.Subdomain),
            new("security_stamp", user.SecurityStamp),
            new("jti", Guid.NewGuid().ToString()),
            new("system_admin", "true"),
            new("can_switch_tenant", "true"),
            new("original_tenant_id", originalTenant.Id.ToString()),
            new("tenant_switched", "true")
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(
                _configuration.GetValue("Auth:TokenExpiryMinutes", 60)),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256),
            Issuer = _configuration["Auth:Issuer"] ?? "RescueRanger",
            Audience = _configuration["Auth:Audience"] ?? "RescueRanger"
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }
    
    private TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = true,
            ValidIssuer = _configuration["Auth:Issuer"] ?? "RescueRanger",
            ValidateAudience = true,
            ValidAudience = _configuration["Auth:Audience"] ?? "RescueRanger",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    }
    
    private static bool IsSystemAdmin(User user)
    {
        // System admin users have a specific role or are in a system tenant
        return user.Role.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase) ||
               user.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}