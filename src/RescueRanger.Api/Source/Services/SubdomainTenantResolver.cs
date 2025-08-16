using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Distributed;
using RescueRanger.Api.Data.Repositories;
using System.Text.Json;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Services;

/// <summary>
/// Resolves tenants based on subdomain extraction from HTTP context
/// </summary>
public class SubdomainTenantResolver : ISubdomainTenantResolver
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubdomainTenantResolver> _logger;
    
    private readonly string _baseDomain;
    private readonly TimeSpan _cacheExpiration;
    private readonly Regex _subdomainRegex;
    
    public SubdomainTenantResolver(
        ITenantRepository tenantRepository,
        IDistributedCache cache,
        IConfiguration configuration,
        ILogger<SubdomainTenantResolver> logger)
    {
        _tenantRepository = tenantRepository;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        
        // Get configuration values
        _baseDomain = _configuration["MultiTenant:BaseDomain"] ?? "rescueranger.com";
        _cacheExpiration = TimeSpan.FromMinutes(_configuration.GetValue<int>("MultiTenant:CacheExpirationMinutes", 30));
        
        // Regex for validating subdomains (alphanumeric, hyphens, 3-63 characters)
        _subdomainRegex = new Regex(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{1,61}[a-zA-Z0-9])?$", RegexOptions.Compiled);
    }
    
    /// <inheritdoc />
    public async Task<TenantInfo?> ResolveTenantAsync(object context)
    {
        if (context is not HttpContext httpContext)
        {
            _logger.LogWarning("Invalid context type for subdomain resolution: {ContextType}", context.GetType());
            return null;
        }
        
        var subdomain = GetTenantIdentifier(context);
        if (string.IsNullOrEmpty(subdomain))
        {
            _logger.LogDebug("No subdomain found in request: {Host}", httpContext.Request.Host);
            return null;
        }
        
        // Try to get from cache first
        var cacheKey = $"tenant:{subdomain}";
        var cachedTenant = await GetFromCacheAsync(cacheKey);
        if (cachedTenant != null)
        {
            _logger.LogDebug("Tenant resolved from cache: {Subdomain}", subdomain);
            return cachedTenant;
        }
        
        // Fetch from database
        var tenant = await _tenantRepository.GetBySubdomainAsync(subdomain);
        if (tenant != null)
        {
            _logger.LogDebug("Tenant resolved from database: {Subdomain}", subdomain);
            
            // Cache the result
            await SetCacheAsync(cacheKey, tenant);
            return tenant;
        }
        
        _logger.LogWarning("Tenant not found for subdomain: {Subdomain}", subdomain);
        return null;
    }
    
    /// <inheritdoc />
    public string? GetTenantIdentifier(object context)
    {
        if (context is not HttpContext httpContext)
            return null;
        
        var host = httpContext.Request.Host.Host;
        return ExtractSubdomain(host);
    }
    
    /// <inheritdoc />
    public string? ExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;
        
        // Handle localhost and IP addresses for development
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || 
            IsIpAddress(host))
        {
            // Check for subdomain in the port or query parameter for development
            return ExtractDevelopmentSubdomain(host);
        }
        
        // Remove port if present
        var hostWithoutPort = host.Split(':')[0];
        
        // Check if it matches our base domain pattern
        var baseDomainPattern = $".{_baseDomain}";
        if (!hostWithoutPort.EndsWith(baseDomainPattern, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Host does not match base domain pattern: {Host} vs {BaseDomain}", 
                hostWithoutPort, _baseDomain);
            return null;
        }
        
        // Extract subdomain
        var subdomain = hostWithoutPort.Substring(0, hostWithoutPort.Length - baseDomainPattern.Length);
        
        return IsValidSubdomain(subdomain) ? subdomain : null;
    }
    
    /// <inheritdoc />
    public bool IsValidSubdomain(string? subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;
        
        // Check against regex pattern
        if (!_subdomainRegex.IsMatch(subdomain))
            return false;
        
        // Exclude reserved subdomains
        var reservedSubdomains = new[] { "www", "api", "admin", "app", "mail", "ftp", "ssl", "cdn" };
        if (reservedSubdomains.Contains(subdomain, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Subdomain is reserved: {Subdomain}", subdomain);
            return false;
        }
        
        return true;
    }
    
    /// <inheritdoc />
    public bool CanAccessTenant(TenantInfo? tenantInfo)
    {
        if (tenantInfo == null)
            return false;
        
        // Check if tenant can be accessed
        var canAccess = tenantInfo.CanAccess;
        
        if (!canAccess)
        {
            _logger.LogWarning("Tenant cannot be accessed: {TenantId} ({Subdomain}), Status: {Status}",
                tenantInfo.Id, tenantInfo.Subdomain, tenantInfo.Status);
        }
        
        return canAccess;
    }
    
    private string? ExtractDevelopmentSubdomain(string host)
    {
        // For development, we might use query parameters or specific conventions
        // This is a placeholder for development-specific logic
        // You could implement logic to extract tenant from:
        // - Query parameters: ?tenant=demo
        // - Headers: X-Tenant-Subdomain
        // - Environment variables
        
        return _configuration["MultiTenant:DevelopmentTenant"] ?? "demo";
    }
    
    private static bool IsIpAddress(string host)
    {
        return System.Net.IPAddress.TryParse(host, out _);
    }
    
    private async Task<TenantInfo?> GetFromCacheAsync(string cacheKey)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (cachedData != null)
            {
                return JsonSerializer.Deserialize<TenantInfo>(cachedData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve tenant from cache: {CacheKey}", cacheKey);
        }
        
        return null;
    }
    
    private async Task SetCacheAsync(string cacheKey, TenantInfo tenant)
    {
        try
        {
            var serializedTenant = JsonSerializer.Serialize(tenant);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            
            await _cache.SetStringAsync(cacheKey, serializedTenant, options);
            _logger.LogDebug("Tenant cached: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache tenant: {CacheKey}", cacheKey);
        }
    }
}