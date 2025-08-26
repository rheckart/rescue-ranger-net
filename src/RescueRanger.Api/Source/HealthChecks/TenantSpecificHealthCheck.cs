using Microsoft.Extensions.Diagnostics.HealthChecks;
using RescueRanger.Api.Data.Repositories;
using RescueRanger.Api.Services;
using System.Diagnostics;

namespace RescueRanger.Api.HealthChecks;

/// <summary>
/// Health check for tenant-specific functionality and database connectivity
/// </summary>
public class TenantSpecificHealthCheck : IHealthCheck
{
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantSpecificHealthCheck> _logger;

    public TenantSpecificHealthCheck(
        ITenantContextService tenantContext,
        ITenantRepository tenantRepository,
        ITenantService tenantService,
        ILogger<TenantSpecificHealthCheck> logger)
    {
        _tenantContext = tenantContext;
        _tenantRepository = tenantRepository;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Test 1: Validate current tenant context
            if (_tenantContext.IsValid)
            {
                data["current_tenant_id"] = _tenantContext.TenantId;
                data["current_tenant_name"] = _tenantContext.TenantName;
                data["current_tenant_subdomain"] = _tenantContext.TenantSubdomain;

                // Test tenant configuration access
                var configStopwatch = Stopwatch.StartNew();
                var tenantConfig = await _tenantContext.GetTenantConfigurationAsync();
                configStopwatch.Stop();
                
                data["tenant_config_query_ms"] = configStopwatch.ElapsedMilliseconds;
                data["tenant_config_available"] = tenantConfig != null;

                // Test tenant access validation
                var accessValidationStopwatch = Stopwatch.StartNew();
                var hasAccess = await _tenantContext.ValidateTenantAccessAsync();
                accessValidationStopwatch.Stop();
                
                data["tenant_access_validation_ms"] = accessValidationStopwatch.ElapsedMilliseconds;
                data["tenant_access_valid"] = hasAccess;

                if (!hasAccess)
                {
                    return HealthCheckResult.Degraded(
                        $"Tenant access validation failed for tenant {_tenantContext.TenantId}",
                        data: data);
                }
            }
            else
            {
                data["tenant_context_established"] = false;
                data["note"] = "No tenant context - this is normal for health check endpoints";
            }

            // Test 2: Database connectivity for tenant queries
            var dbStopwatch = Stopwatch.StartNew();
            var activeTenants = await _tenantRepository.GetActiveTenantsCountAsync();
            dbStopwatch.Stop();

            data["active_tenants_count"] = activeTenants;
            data["tenant_db_query_ms"] = dbStopwatch.ElapsedMilliseconds;

            if (dbStopwatch.ElapsedMilliseconds > 500)
            {
                return HealthCheckResult.Degraded(
                    $"Tenant database queries slow ({dbStopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }

            // Test 3: Tenant service functionality
            var serviceStopwatch = Stopwatch.StartNew();
            try
            {
                // Test getting tenant by subdomain (using a known test tenant)
                var testResult = await _tenantRepository.GetBySubdomainAsync("healthcheck-test");
                serviceStopwatch.Stop();
                
                data["tenant_service_query_ms"] = serviceStopwatch.ElapsedMilliseconds;
                data["tenant_service_responsive"] = true;
            }
            catch (Exception ex)
            {
                serviceStopwatch.Stop();
                data["tenant_service_responsive"] = false;
                data["tenant_service_error"] = ex.Message;
                
                _logger.LogWarning(ex, "Tenant service health check failed");
            }

            stopwatch.Stop();
            data["total_health_check_ms"] = stopwatch.ElapsedMilliseconds;

            return HealthCheckResult.Healthy(
                "Tenant-specific health check passed",
                data: data);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            data["total_health_check_ms"] = stopwatch.ElapsedMilliseconds;
            
            _logger.LogError(ex, "Tenant-specific health check failed");
            
            return HealthCheckResult.Unhealthy(
                "Tenant-specific health check failed",
                exception: ex,
                data: data);
        }
    }
}

/// <summary>
/// Health check for tenant configuration validation
/// </summary>
public class TenantConfigurationHealthCheck : IHealthCheck
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<TenantConfigurationHealthCheck> _logger;

    public TenantConfigurationHealthCheck(
        ITenantRepository tenantRepository,
        ILogger<TenantConfigurationHealthCheck> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        try
        {
            // Test 1: Check for misconfigured tenants
            var stopwatch = Stopwatch.StartNew();
            var allTenants = await _tenantRepository.GetAllTenantsAsync();
            stopwatch.Stop();

            data["total_tenants"] = allTenants.Count;
            data["query_time_ms"] = stopwatch.ElapsedMilliseconds;

            var activeTenants = allTenants.Where(t => t.Status == TenantStatus.Active).ToList();
            var suspendedTenants = allTenants.Where(t => t.Status == TenantStatus.Suspended).ToList();
            var archivedTenants = allTenants.Where(t => t.Status == TenantStatus.Archived).ToList();

            data["active_tenants"] = activeTenants.Count;
            data["suspended_tenants"] = suspendedTenants.Count;
            data["archived_tenants"] = archivedTenants.Count;

            // Test 2: Check for configuration issues
            var configurationIssues = new List<string>();

            // Check for duplicate subdomains
            var duplicateSubdomains = allTenants
                .GroupBy(t => t.Subdomain.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateSubdomains.Any())
            {
                configurationIssues.Add($"Duplicate subdomains found: {string.Join(", ", duplicateSubdomains)}");
            }

            // Check for tenants with missing or invalid configurations
            var tenantsWithoutConfig = activeTenants.Where(t => string.IsNullOrWhiteSpace(t.Name)).ToList();
            if (tenantsWithoutConfig.Any())
            {
                configurationIssues.Add($"{tenantsWithoutConfig.Count} active tenants have missing names");
            }

            // Check for expired tenants that should be archived
            var expiredTenants = activeTenants
                .Where(t => t.CreatedAt < DateTime.UtcNow.AddYears(-2)) // Example: tenants older than 2 years
                .ToList();

            data["potentially_expired_tenants"] = expiredTenants.Count;

            // Test 3: Performance checks
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                configurationIssues.Add($"Tenant configuration query took {stopwatch.ElapsedMilliseconds}ms (threshold: 1000ms)");
            }

            data["configuration_issues"] = configurationIssues;

            if (configurationIssues.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Tenant configuration issues detected: {string.Join("; ", configurationIssues)}",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Tenant configuration is valid",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant configuration health check failed");
            
            return HealthCheckResult.Unhealthy(
                "Tenant configuration health check failed",
                exception: ex,
                data: data);
        }
    }
}

/// <summary>
/// Health check for tenant resolution performance metrics
/// </summary>
public class TenantResolutionPerformanceHealthCheck : IHealthCheck
{
    private readonly TenantResolutionMetrics _metrics;
    private readonly ILogger<TenantResolutionPerformanceHealthCheck> _logger;

    public TenantResolutionPerformanceHealthCheck(
        TenantResolutionMetrics metrics,
        ILogger<TenantResolutionPerformanceHealthCheck> logger)
    {
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Synchronous operation but maintain async signature

        var data = new Dictionary<string, object>();

        try
        {
            // Get current metrics
            var recentMetrics = _metrics.GetRecentMetrics();
            var totalRequests = recentMetrics.TotalRequests;
            var successfulRequests = recentMetrics.SuccessCount;
            var failedRequests = recentMetrics.FailureCount;
            var averageResolutionTime = recentMetrics.AverageDurationMs;
            var successRate = recentMetrics.SuccessRate;

            data["total_resolution_attempts"] = totalRequests;
            data["successful_resolutions"] = successfulRequests;
            data["failed_resolutions"] = failedRequests;
            data["average_resolution_time_ms"] = averageResolutionTime;
            data["success_rate_percent"] = successRate;

            // Performance thresholds
            var issues = new List<string>();

            if (successRate < 95.0) // 95% success rate threshold
            {
                issues.Add($"Low success rate: {successRate:F2}%");
            }

            if (averageResolutionTime > 100) // 100ms threshold
            {
                issues.Add($"High resolution time: {averageResolutionTime:F2}ms");
            }

            data["performance_issues"] = issues;

            if (issues.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Tenant resolution performance issues: {string.Join("; ", issues)}",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Tenant resolution performance is optimal",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant resolution performance health check failed");
            
            return HealthCheckResult.Unhealthy(
                "Tenant resolution performance health check failed",
                exception: ex,
                data: data);
        }
    }
}