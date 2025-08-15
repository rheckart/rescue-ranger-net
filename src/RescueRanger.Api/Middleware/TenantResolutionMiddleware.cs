using Microsoft.Extensions.Options;
using RescueRanger.Core.Models;
using RescueRanger.Core.Repositories;
using RescueRanger.Core.Services;
using System.Text.RegularExpressions;

namespace RescueRanger.Api.Middleware;

/// <summary>
/// Middleware for resolving tenant context from incoming requests
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly MultiTenantOptions _options;
    private static readonly Regex SubdomainRegex = new(@"^([a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IOptions<MultiTenantOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant resolution for health endpoints
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Get services from DI container
        var tenantContextService = context.RequestServices.GetRequiredService<ITenantContextService>();
        var tenantRepository = context.RequestServices.GetRequiredService<ITenantRepository>();

        try
        {
            // Try to resolve tenant from multiple sources
            var tenantIdentifier = await ResolveTenantIdentifierAsync(context);
            
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                _logger.LogDebug("Tenant identifier resolved: {TenantIdentifier}", tenantIdentifier);
                
                // Look up tenant in database
                var tenant = await tenantRepository.GetBySubdomainAsync(tenantIdentifier);
                
                if (tenant != null)
                {
                    // Convert to TenantInfo
                    var tenantInfo = new TenantInfo
                    {
                        Id = tenant.Id,
                        Name = tenant.Name,
                        Subdomain = tenant.Subdomain,
                        Status = tenant.Status,
                        CreatedAt = tenant.CreatedAt,
                        Configuration = tenant.Configuration
                    };
                    
                    // Set tenant context
                    tenantContextService.SetTenant(tenantInfo);
                    
                    _logger.LogInformation("Tenant context set for {TenantName} ({TenantId})", 
                        tenant.Name, tenant.Id);
                    
                    // Validate tenant access
                    if (!await tenantContextService.ValidateTenantAccessAsync())
                    {
                        _logger.LogWarning("Tenant access denied for {TenantName} ({TenantId})", 
                            tenant.Name, tenant.Id);
                        
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Tenant access denied");
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("Tenant not found for identifier: {TenantIdentifier}", tenantIdentifier);
                    
                    // In production, you might want to return 404 or redirect to a default page
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync("Tenant not found");
                    return;
                }
            }
            else if (!IsDevelopmentMode(context))
            {
                // In production, tenant resolution is required
                _logger.LogWarning("No tenant identifier found in request");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Tenant identifier required");
                return;
            }
            else
            {
                // In development mode, use default tenant if configured
                if (!string.IsNullOrWhiteSpace(_options.DevelopmentTenant))
                {
                    var devTenant = await tenantRepository.GetBySubdomainAsync(_options.DevelopmentTenant);
                    if (devTenant != null)
                    {
                        var tenantInfo = new TenantInfo
                        {
                            Id = devTenant.Id,
                            Name = devTenant.Name,
                            Subdomain = devTenant.Subdomain,
                            Status = devTenant.Status,
                            CreatedAt = devTenant.CreatedAt,
                            Configuration = devTenant.Configuration
                        };
                        
                        tenantContextService.SetTenant(tenantInfo);
                        _logger.LogDebug("Development tenant set: {TenantName}", devTenant.Name);
                    }
                }
            }

            // Continue processing
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving tenant context");
            
            // Clear any partial tenant context
            tenantContextService.Clear();
            
            // Return error response
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Error processing tenant context");
        }
        finally
        {
            // Clear tenant context after request (important for thread safety)
            tenantContextService.Clear();
        }
    }

    /// <summary>
    /// Resolves tenant identifier from various sources
    /// </summary>
    private Task<string?> ResolveTenantIdentifierAsync(HttpContext context)
    {
        string? tenantIdentifier = null;
        
        // Priority 1: Try subdomain resolution
        tenantIdentifier = ExtractSubdomainFromHost(context.Request.Host.Host);
        if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            _logger.LogDebug("Tenant resolved from subdomain: {TenantIdentifier}", tenantIdentifier);
            return Task.FromResult(tenantIdentifier);
        }
        
        // Priority 2: Try header-based resolution (X-Tenant-Id or X-Tenant-Subdomain)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            tenantIdentifier = tenantIdHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                _logger.LogDebug("Tenant resolved from X-Tenant-Id header: {TenantIdentifier}", tenantIdentifier);
                return Task.FromResult(tenantIdentifier);
            }
        }
        
        if (context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var tenantSubdomainHeader))
        {
            tenantIdentifier = tenantSubdomainHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                _logger.LogDebug("Tenant resolved from X-Tenant-Subdomain header: {TenantIdentifier}", tenantIdentifier);
                return Task.FromResult(tenantIdentifier);
            }
        }
        
        // Priority 3: Try query parameter resolution
        if (context.Request.Query.TryGetValue("tenant", out var tenantQuery))
        {
            tenantIdentifier = tenantQuery.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                _logger.LogDebug("Tenant resolved from query parameter: {TenantIdentifier}", tenantIdentifier);
                return Task.FromResult(tenantIdentifier);
            }
        }
        
        // Priority 4: Try route data (if using route-based tenancy)
        if (context.Request.RouteValues.TryGetValue("tenant", out var tenantRoute))
        {
            tenantIdentifier = tenantRoute?.ToString();
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                _logger.LogDebug("Tenant resolved from route: {TenantIdentifier}", tenantIdentifier);
                return Task.FromResult(tenantIdentifier);
            }
        }
        
        return Task.FromResult(tenantIdentifier);
    }
    
    /// <summary>
    /// Extracts subdomain from the host
    /// </summary>
    private string? ExtractSubdomainFromHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;
        
        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];
        
        // Check if it's localhost or IP address
        if (hostWithoutPort.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            System.Net.IPAddress.TryParse(hostWithoutPort, out _))
        {
            return null;
        }
        
        // Extract subdomain
        var baseDomain = _options.BaseDomain;
        if (hostWithoutPort.EndsWith($".{baseDomain}", StringComparison.OrdinalIgnoreCase))
        {
            var subdomain = hostWithoutPort[..^(baseDomain.Length + 1)];
            
            // Validate subdomain format and check if it's not reserved
            if (IsValidSubdomain(subdomain) && !IsReservedSubdomain(subdomain))
            {
                return subdomain.ToLowerInvariant();
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Validates subdomain format
    /// </summary>
    private bool IsValidSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;
        
        // Check length (3-63 characters)
        if (subdomain.Length < 3 || subdomain.Length > 63)
            return false;
        
        // Check format using regex
        return SubdomainRegex.IsMatch(subdomain);
    }
    
    /// <summary>
    /// Checks if subdomain is reserved
    /// </summary>
    private bool IsReservedSubdomain(string subdomain)
    {
        return _options.ReservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Checks if running in development mode
    /// </summary>
    private bool IsDevelopmentMode(HttpContext context)
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        return env.IsDevelopment() && _options.EnableInDevelopment;
    }
}

/// <summary>
/// Extension methods for registering tenant resolution middleware
/// </summary>
public static class TenantResolutionMiddlewareExtensions
{
    /// <summary>
    /// Adds tenant resolution middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantResolutionMiddleware>();
    }
}