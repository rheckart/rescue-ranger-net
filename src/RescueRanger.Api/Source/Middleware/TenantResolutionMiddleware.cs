using Microsoft.Extensions.Options;
using RescueRanger.Api.Data.Repositories;
using RescueRanger.Api.Services;
using System.Text.RegularExpressions;
using Ardalis.Result;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api1.Middleware;

/// <summary>
/// Middleware for resolving tenant context from incoming requests
/// </summary>
public partial class TenantResolutionMiddleware(
    RequestDelegate next,
    ILogger<TenantResolutionMiddleware> logger,
    IOptions<MultiTenantOptions> options)
{
    private readonly MultiTenantOptions _options = options.Value;
    private static readonly Regex _subdomainRegex = SubdomainRegex();

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant resolution for health endpoints
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
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
                logger.LogDebug("Tenant identifier resolved: {TenantIdentifier}", tenantIdentifier);
                
                // Look up tenant in database
                var tenantResult = await tenantRepository.GetBySubdomainAsync(tenantIdentifier);
                
                if (tenantResult.IsSuccess)
                {
                    var tenant = tenantResult.Value;
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
                    
                    logger.LogInformation("Tenant context set for {TenantName} ({TenantId})", 
                        tenant.Name, tenant.Id);
                    
                    // Validate tenant access
                    if (!await tenantContextService.ValidateTenantAccessAsync())
                    {
                        logger.LogWarning("Tenant access denied for {TenantName} ({TenantId})", 
                            tenant.Name, tenant.Id);
                        
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync("Tenant access denied");
                        return;
                    }
                }
                else
                {
                    logger.LogWarning("Tenant not found for identifier: {TenantIdentifier}", tenantIdentifier);
                    
                    // In production, you might want to return 404 or redirect to a default page
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync("Tenant not found");
                    return;
                }
            }
            else if (!IsDevelopmentMode(context))
            {
                // In production, tenant resolution is required
                logger.LogWarning("No tenant identifier found in request");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Tenant identifier required");
                return;
            }
            else
            {
                // In development mode, use default tenant if configured
                if (!string.IsNullOrWhiteSpace(_options.DevelopmentTenant))
                {
                    var devTenantResult = await tenantRepository.GetBySubdomainAsync(_options.DevelopmentTenant);
                    if (devTenantResult.IsSuccess)
                    {
                        var devTenant = devTenantResult.Value;
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
                        logger.LogDebug("Development tenant set: {TenantName}", devTenant.Name);
                    }
                }
            }

            // Continue processing
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resolving tenant context");
            
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
    private Task<Result<string>> ResolveTenantIdentifierAsync(HttpContext context)
    {
        var tenantIdentifier =
            // Priority 1: Try subdomain resolution
            ExtractSubdomainFromHost(context.Request.Host.Host);
        if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            logger.LogDebug("Tenant resolved from subdomain: {TenantIdentifier}", tenantIdentifier);
            return Task.FromResult(Result.Success(tenantIdentifier));
        }
        
        // Priority 2: Try header-based resolution (X-Tenant-Id or X-Tenant-Subdomain)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            tenantIdentifier = tenantIdHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                logger.LogDebug("Tenant resolved from X-Tenant-Id header: {TenantIdentifier}", tenantIdentifier);
                return Task.FromResult(Result.Success(tenantIdentifier));
            }
        }
        
        if (context.Request.Headers.TryGetValue("X-Tenant-Subdomain", out var tenantSubdomainHeader))
        {
            tenantIdentifier = tenantSubdomainHeader.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                logger.LogDebug("Tenant resolved from X-Tenant-Subdomain header: {TenantIdentifier}", tenantIdentifier);
                return Task.FromResult(Result.Success(tenantIdentifier));
            }
        }
        
        // Priority 3: Try query parameter resolution
        if (context.Request.Query.TryGetValue("tenant", out var tenantQuery))
        {
            tenantIdentifier = tenantQuery.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                logger.LogDebug("Tenant resolved from query parameter: {TenantIdentifier}", tenantIdentifier);
                return Task.FromResult(Result.Success(tenantIdentifier));
            }
        }
        
        // Priority 4: Try route data (if using route-based tenancy)
        if (!context.Request.RouteValues.TryGetValue("tenant", out var tenantRoute))
            return Task.FromResult<Result<string>>(Result.NotFound());
        
        tenantIdentifier = tenantRoute?.ToString();
            
        if (string.IsNullOrWhiteSpace(tenantIdentifier)) return Task.FromResult<Result<string>>(Result.NotFound());
        
        logger.LogDebug("Tenant resolved from route: {TenantIdentifier}", tenantIdentifier);
        return Task.FromResult(Result.Success(tenantIdentifier));
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
        if (!hostWithoutPort.EndsWith($".{baseDomain}", StringComparison.OrdinalIgnoreCase)) return null;
        var subdomain = hostWithoutPort[..^(baseDomain.Length + 1)];
            
        // Validate subdomain format and check if it's not reserved
        if (IsValidSubdomain(subdomain) && !IsReservedSubdomain(subdomain))
        {
            return subdomain.ToLowerInvariant();
        }

        return null;
    }
    
    /// <summary>
    /// Validates subdomain format
    /// </summary>
    private static bool IsValidSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;

        // Check length (3-63 characters)
        return subdomain.Length is >= 3 and <= 63 &&
               // Check format using regex
               _subdomainRegex.IsMatch(subdomain);
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

    [GeneratedRegex(@"^([a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex SubdomainRegex();
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