using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RescueRanger.Api.Data.Repositories;
using RescueRanger.Api.Entities;
using RescueRanger.Api.Services;
using System.Diagnostics;

namespace RescueRanger.Api.HealthChecks;

/// <summary>
/// Health check for tenant resolution functionality
/// </summary>
public class TenantResolutionHealthCheck : IHealthCheck
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContextService _tenantContextService;
    private readonly IDistributedCache _cache;
    private readonly MultiTenantOptions _options;
    private readonly ILogger<TenantResolutionHealthCheck> _logger;

    public TenantResolutionHealthCheck(
        ITenantRepository tenantRepository,
        ITenantContextService tenantContextService,
        IDistributedCache cache,
        IOptions<MultiTenantOptions> options,
        ILogger<TenantResolutionHealthCheck> logger)
    {
        _tenantRepository = tenantRepository;
        _tenantContextService = tenantContextService;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        
        try
        {
            // Test 1: Check if we can resolve the development tenant
            if (!string.IsNullOrWhiteSpace(_options.DevelopmentTenant))
            {
                var stopwatch = Stopwatch.StartNew();
                var tenantResult = await _tenantRepository.GetBySubdomainAsync(_options.DevelopmentTenant);
                stopwatch.Stop();
                
                data["development_tenant_resolution_ms"] = stopwatch.ElapsedMilliseconds;
                
                if (!tenantResult.IsSuccess)
                {
                    return HealthCheckResult.Degraded(
                        $"Development tenant '{_options.DevelopmentTenant}' not found",
                        data: data);
                }
                
                data["development_tenant"] = _options.DevelopmentTenant;
                data["development_tenant_status"] = tenantResult.Value.Status.ToString();
            }
            
            // Test 2: Check cache connectivity
            try
            {
                var cacheKey = "health:tenant-resolution:test";
                await _cache.SetStringAsync(cacheKey, DateTime.UtcNow.ToString("O"), 
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                    }, 
                    cancellationToken);
                
                var cachedValue = await _cache.GetStringAsync(cacheKey, cancellationToken);
                data["cache_available"] = !string.IsNullOrEmpty(cachedValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache not available for tenant resolution");
                data["cache_available"] = false;
                data["cache_error"] = ex.Message;
            }
            
            // Test 3: Check tenant context service
            var contextServiceHealthy = _tenantContextService.CurrentTenant == null || 
                                       _tenantContextService.IsValidTenant;
            data["context_service_healthy"] = contextServiceHealthy;
            
            if (!contextServiceHealthy)
            {
                return HealthCheckResult.Degraded(
                    "Tenant context service in invalid state",
                    data: data);
            }
            
            // Test 4: Check configuration
            data["base_domain"] = _options.BaseDomain;
            data["reserved_subdomains_count"] = _options.ReservedSubdomains.Count;
            data["cache_expiration_minutes"] = _options.CacheExpirationMinutes;
            data["development_mode_enabled"] = _options.EnableInDevelopment;
            
            // Test 5: Check database connectivity for tenant queries
            var dbStopwatch = Stopwatch.StartNew();
            var activeTenants = await _tenantRepository.GetActiveTenantsCountAsync();
            dbStopwatch.Stop();
            
            data["active_tenants_count"] = activeTenants;
            data["database_query_ms"] = dbStopwatch.ElapsedMilliseconds;
            
            if (dbStopwatch.ElapsedMilliseconds > 100)
            {
                return HealthCheckResult.Degraded(
                    $"Database queries slow ({dbStopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }
            
            return HealthCheckResult.Healthy(
                "Tenant resolution is functioning properly",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant resolution health check failed");
            
            return HealthCheckResult.Unhealthy(
                "Tenant resolution health check failed",
                exception: ex,
                data: data);
        }
    }
}