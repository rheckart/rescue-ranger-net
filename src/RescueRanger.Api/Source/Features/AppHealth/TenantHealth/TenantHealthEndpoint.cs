using Microsoft.Extensions.Diagnostics.HealthChecks;
using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.AppHealth.TenantHealth;

/// <summary>
/// Endpoint for checking tenant-specific health
/// </summary>
public sealed class TenantHealthEndpoint : Endpoint<TenantHealthRequest, TenantHealthResponse>
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantAuditService _auditService;
    private readonly ILogger<TenantHealthEndpoint> _logger;

    public TenantHealthEndpoint(
        HealthCheckService healthCheckService,
        ITenantContextService tenantContext,
        ITenantAuditService auditService,
        ILogger<TenantHealthEndpoint> logger)
    {
        _healthCheckService = healthCheckService;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/health/tenant");
        Summary(s =>
        {
            s.Summary = "Check tenant-specific health";
            s.Description = "Performs health checks specific to the current tenant context including database connectivity and configuration validation";
            s.Responses[200] = "Tenant health check results";
            s.Responses[503] = "Service unavailable - health checks failed";
        });
        
        // Require valid tenant context but allow any authenticated user
        Policies(TenantAuthorizationPolicies.TenantUser);
    }

    public override async Task HandleAsync(TenantHealthRequest req, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Run tenant-specific health checks
            var healthCheckResult = await _healthCheckService.CheckHealthAsync(ct);

            var tenantHealthResponse = new TenantHealthResponse
            {
                Status = healthCheckResult.Status.ToString(),
                TenantId = _tenantContext.TenantId,
                TenantName = _tenantContext.TenantName,
                TenantSubdomain = _tenantContext.TenantSubdomain,
                CheckedAt = startTime,
                Duration = DateTime.UtcNow - startTime,
                Checks = new Dictionary<string, TenantHealthCheckResult>()
            };

            // Process individual health check results
            foreach (var (key, value) in healthCheckResult.Entries)
            {
                tenantHealthResponse.Checks[key] = new TenantHealthCheckResult
                {
                    Status = value.Status.ToString(),
                    Duration = value.Duration,
                    Description = value.Description ?? string.Empty,
                    Data = value.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Exception = value.Exception?.Message
                };
            }

            // Log tenant health check
            await _auditService.LogTenantAccessAsync(new TenantAccessEvent
            {
                TenantId = _tenantContext.TenantId,
                UserId = HttpContext.User.Identity?.IsAuthenticated == true 
                    ? HttpContext.RequestServices.GetRequiredService<ITenantUserIdentityService>().GetCurrentUserId() 
                    : null,
                UserEmail = HttpContext.User.Identity?.IsAuthenticated == true 
                    ? HttpContext.RequestServices.GetRequiredService<ITenantUserIdentityService>().GetCurrentUserEmail() 
                    : "anonymous",
                RequestId = HttpContext.TraceIdentifier,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                Endpoint = "/health/tenant",
                HttpMethod = "GET",
                StatusCode = healthCheckResult.Status == HealthStatus.Healthy ? 200 : 503,
                ResponseTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                AdditionalData = new Dictionary<string, object>
                {
                    ["health_status"] = healthCheckResult.Status.ToString(),
                    ["checks_count"] = healthCheckResult.Entries.Count
                }
            });

            // Send appropriate HTTP response
            if (healthCheckResult.Status == HealthStatus.Healthy || healthCheckResult.Status == HealthStatus.Degraded)
            {
                await Send.OkAsync(tenantHealthResponse, ct);
            }
            else
            {
                await Send.ResponseAsync(tenantHealthResponse, 503, ct);
            }

            _logger.LogInformation("Tenant health check completed for {TenantId} with status {Status}",
                _tenantContext.TenantId, healthCheckResult.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing tenant health check for tenant {TenantId}",
                _tenantContext.TenantId);

            var errorResponse = new TenantHealthResponse
            {
                Status = "Unhealthy",
                TenantId = _tenantContext.TenantId,
                TenantName = _tenantContext.TenantName,
                TenantSubdomain = _tenantContext.TenantSubdomain,
                CheckedAt = startTime,
                Duration = DateTime.UtcNow - startTime,
                Checks = new Dictionary<string, TenantHealthCheckResult>
                {
                    ["error"] = new TenantHealthCheckResult
                    {
                        Status = "Unhealthy",
                        Description = "Health check failed with exception",
                        Exception = ex.Message,
                        Duration = DateTime.UtcNow - startTime
                    }
                }
            };

            await Send.ResponseAsync(errorResponse, 503, ct);
        }
    }
}

/// <summary>
/// Request model for tenant health check
/// </summary>
public sealed class TenantHealthRequest
{
    /// <summary>
    /// Whether to include detailed check information
    /// </summary>
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Specific checks to run (if empty, runs all)
    /// </summary>
    public List<string> Checks { get; set; } = new();
}

/// <summary>
/// Response model for tenant health check
/// </summary>
public sealed class TenantHealthResponse
{
    /// <summary>
    /// Overall health status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Current tenant ID
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Current tenant name
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Current tenant subdomain
    /// </summary>
    public string TenantSubdomain { get; set; } = string.Empty;

    /// <summary>
    /// When the health check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; }

    /// <summary>
    /// How long the health check took
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Individual health check results
    /// </summary>
    public Dictionary<string, TenantHealthCheckResult> Checks { get; set; } = new();
}

/// <summary>
/// Individual health check result
/// </summary>
public sealed class TenantHealthCheckResult
{
    /// <summary>
    /// Status of this specific check
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Duration of this specific check
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Description of the check or any issues
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional data from the check
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Exception message if the check failed
    /// </summary>
    public string? Exception { get; set; }
}