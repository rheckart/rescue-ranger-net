using Ardalis.Result;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Entities;
using RescueRanger.Infrastructure.Data;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for tenant-aware user authentication
/// </summary>
public class TenantAuthenticationService : ITenantAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContextService _tenantContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantAuthenticationService> _logger;
    
    public TenantAuthenticationService(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        IJwtTokenService jwtTokenService,
        ITenantService tenantService,
        ILogger<TenantAuthenticationService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _jwtTokenService = jwtTokenService;
        _tenantService = tenantService;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<Result<TenantAuthenticationResult>> AuthenticateAsync(string email, string password)
    {
        if (!_tenantContext.IsValid)
            return Result<TenantAuthenticationResult>.Forbidden("Tenant context not established");
            
        try
        {
            // Find user within current tenant context
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
                
            if (user is null)
                return Result<TenantAuthenticationResult>.NotFound("User not found");
                
            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                await IncrementFailedAttemptsAsync(user);
                return Result<TenantAuthenticationResult>.Forbidden("Invalid credentials");
            }
            
            // Check if user is locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                return Result<TenantAuthenticationResult>.Forbidden(
                    $"User is locked out until {user.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC");
            }
            
            // Check if user is active
            if (!user.IsActive)
                return Result<TenantAuthenticationResult>.Forbidden("User account is deactivated");
                
            // Reset failed attempts on successful login
            await ResetFailedAttemptsAsync(user);
            
            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            // Generate tokens
            var tenantInfo = _tenantContext.CurrentTenant!;
            var accessToken = _jwtTokenService.GenerateToken(user, tenantInfo);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            
            // Save refresh token
            user.RefreshTokens.Add(refreshToken);
            
            // Clean up old refresh tokens
            await CleanupExpiredRefreshTokensAsync(user);
            await _context.SaveChangesAsync();
            
            var result = new TenantAuthenticationResult
            {
                User = user,
                Tenant = tenantInfo,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                TokenExpiresAt = DateTime.UtcNow.AddMinutes(60), // From configuration
                IsTenantSwitch = false
            };
            
            _logger.LogInformation("User {Email} authenticated successfully for tenant {TenantId}", 
                user.Email, tenantInfo.Id);
                
            return Result<TenantAuthenticationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for email {Email}", email);
            return Result<TenantAuthenticationResult>.Error("Authentication failed");
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<TenantAuthenticationResult>> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        if (!_tenantContext.IsValid)
            return Result<TenantAuthenticationResult>.Forbidden("Tenant context not established");
            
        try
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken));
                
            if (user is null)
                return Result<TenantAuthenticationResult>.NotFound("Invalid refresh token");
                
            var existingToken = user.RefreshTokens.First(rt => rt.Token == refreshToken);
            
            if (!existingToken.IsActive)
                return Result<TenantAuthenticationResult>.Forbidden("Refresh token expired or revoked");
                
            // Revoke the used refresh token
            existingToken.RevokedAt = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            
            // Generate new tokens
            var tenantInfo = _tenantContext.CurrentTenant!;
            var newAccessToken = _jwtTokenService.GenerateToken(user, tenantInfo);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken(ipAddress);
            
            // Link the new token to the old one
            newRefreshToken.ReplacedByToken = existingToken.Token;
            existingToken.ReplacedByToken = newRefreshToken.Token;
            
            user.RefreshTokens.Add(newRefreshToken);
            await CleanupExpiredRefreshTokensAsync(user);
            await _context.SaveChangesAsync();
            
            var result = new TenantAuthenticationResult
            {
                User = user,
                Tenant = tenantInfo,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                TokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
                IsTenantSwitch = false
            };
            
            return Result<TenantAuthenticationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return Result<TenantAuthenticationResult>.Error("Token refresh failed");
        }
    }
    
    /// <inheritdoc />
    public async Task<Result> RevokeTokenAsync(string refreshToken, string? ipAddress = null)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken));
                
            if (user is null)
                return Result.NotFound("Invalid refresh token");
                
            var token = user.RefreshTokens.First(rt => rt.Token == refreshToken);
            
            if (!token.IsActive)
                return Result.Forbidden("Token already revoked or expired");
                
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            
            await _context.SaveChangesAsync();
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token revocation failed");
            return Result.Error("Token revocation failed");
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> ValidateUserTenantAccessAsync(Guid userId)
    {
        if (!_tenantContext.IsValid)
            return false;
            
        try
        {
            // Check if user exists in current tenant
            return await _context.Users
                .AnyAsync(u => u.Id == userId && u.TenantId == _tenantContext.TenantId && u.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User tenant access validation failed for user {UserId}", userId);
            return false;
        }
    }
    
    /// <inheritdoc />
    public async Task<Result<TenantAuthenticationResult>> SwitchTenantAsync(Guid userId, Guid targetTenantId)
    {
        try
        {
            // Get user from current tenant (bypass tenant filter for system admins)
            var user = await _context.AllTenants<User>()
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user is null)
                return Result<TenantAuthenticationResult>.NotFound("User not found");
                
            // Verify user is system admin
            if (!IsSystemAdmin(user))
                return Result<TenantAuthenticationResult>.Forbidden("Only system administrators can switch tenants");
                
            // Get target tenant
            var targetTenant = await _tenantService.GetTenantInfoAsync(targetTenantId);
            if (targetTenant is null)
                return Result<TenantAuthenticationResult>.NotFound("Target tenant not found");
                
            // Store original tenant info
            var originalTenant = _tenantContext.CurrentTenant!;
            
            // Generate tenant switch token
            var accessToken = _jwtTokenService.GenerateTenantSwitchToken(user, originalTenant, targetTenant);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            
            var result = new TenantAuthenticationResult
            {
                User = user,
                Tenant = targetTenant,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                TokenExpiresAt = DateTime.UtcNow.AddMinutes(60),
                IsTenantSwitch = true,
                OriginalTenantId = originalTenant.Id
            };
            
            _logger.LogInformation("System admin {Email} switched from tenant {OriginalTenant} to {TargetTenant}",
                user.Email, originalTenant.Id, targetTenant.Id);
                
            return Result<TenantAuthenticationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant switching failed for user {UserId} to tenant {TargetTenantId}", userId, targetTenantId);
            return Result<TenantAuthenticationResult>.Error("Tenant switching failed");
        }
    }
    
    private async Task IncrementFailedAttemptsAsync(User user)
    {
        user.AccessFailedCount++;
        
        // Lock user after 5 failed attempts
        if (user.AccessFailedCount >= 5)
        {
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(30); // 30-minute lockout
            _logger.LogWarning("User {Email} locked out due to failed login attempts", user.Email);
        }
        
        await _context.SaveChangesAsync();
    }
    
    private async Task ResetFailedAttemptsAsync(User user)
    {
        if (user.AccessFailedCount > 0)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
        }
    }
    
    private async Task CleanupExpiredRefreshTokensAsync(User user)
    {
        // Remove expired refresh tokens (keep only last 5 active tokens)
        var expiredTokens = user.RefreshTokens
            .Where(rt => !rt.IsActive)
            .OrderBy(rt => rt.CreatedAt)
            .Take(user.RefreshTokens.Count - 5)
            .ToList();
            
        foreach (var token in expiredTokens)
        {
            user.RefreshTokens.Remove(token);
        }
        
        await Task.CompletedTask;
    }
    
    private static bool IsSystemAdmin(User user)
    {
        return user.Role.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase) ||
               user.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}