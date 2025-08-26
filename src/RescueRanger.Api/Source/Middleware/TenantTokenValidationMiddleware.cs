using System.Security.Claims;
using RescueRanger.Api.Services;

namespace RescueRanger.Api1.Middleware;

/// <summary>
/// Middleware to validate and extract tenant information from JWT tokens
/// </summary>
public class TenantTokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantTokenValidationMiddleware> _logger;
    
    public TenantTokenValidationMiddleware(RequestDelegate next, ILogger<TenantTokenValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, ITenantContextService tenantContext, IJwtTokenService jwtTokenService)
    {
        try
        {
            // Skip processing for non-authenticated requests
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }
            
            // Extract tenant information from token claims
            var tenantInfo = jwtTokenService.ExtractTenantFromClaims(context.User);
            if (tenantInfo.HasValue)
            {
                var (tenantId, tenantName) = tenantInfo.Value;
                
                // Validate that token tenant matches URL tenant
                if (tenantContext.IsValid && tenantContext.TenantId != tenantId)
                {
                    _logger.LogWarning("Token tenant {TokenTenantId} does not match URL tenant {UrlTenantId}",
                        tenantId, tenantContext.TenantId);
                        
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Token tenant mismatch");
                    return;
                }
                
                // Update tenant context with token information if not already set
                if (!tenantContext.IsValid && tenantId.HasValue && tenantName is not null)
                {
                    var tenantSubdomain = context.User.FindFirstValue("tenant_subdomain") ?? "unknown";
                    tenantContext.SetTenant(tenantId.Value, tenantSubdomain, tenantName);
                }
            }
            
            // Check for tenant switching
            var isTenantSwitched = context.User.FindFirstValue("tenant_switched");
            if (!string.IsNullOrEmpty(isTenantSwitched) && bool.TryParse(isTenantSwitched, out var switched) && switched)
            {
                // Add claim to indicate this is a switched tenant session
                var identity = context.User.Identity as ClaimsIdentity;
                identity?.AddClaim(new Claim("is_tenant_switched", "true"));
                
                var originalTenantId = context.User.FindFirstValue("original_tenant_id");
                if (!string.IsNullOrEmpty(originalTenantId))
                {
                    identity?.AddClaim(new Claim("original_tenant", originalTenantId));
                }
            }
            
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tenant token validation middleware");
            
            // Don't fail the entire request for token validation errors
            // Let the authentication middleware handle it
            await _next(context);
        }
    }
}

/// <summary>
/// Extension methods for registering tenant token validation middleware
/// </summary>
public static class TenantTokenValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds tenant token validation middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseTenantTokenValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantTokenValidationMiddleware>();
    }
}