using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.AppHealth.TenantMetrics;

/// <summary>
/// Endpoint for retrieving tenant performance and usage metrics
/// </summary>
[RequireTenantAdmin]
public sealed class TenantMetricsEndpoint : Endpoint<TenantMetricsRequest, TenantMetricsResponse>
{
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantAuditService _auditService;
    private readonly TenantResolutionMetrics _resolutionMetrics;
    private readonly ILogger<TenantMetricsEndpoint> _logger;

    public TenantMetricsEndpoint(
        ITenantContextService tenantContext,
        ITenantAuditService auditService,
        TenantResolutionMetrics resolutionMetrics,
        ILogger<TenantMetricsEndpoint> logger)
    {
        _tenantContext = tenantContext;
        _auditService = auditService;
        _resolutionMetrics = resolutionMetrics;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/health/tenant/metrics");
        Summary(s =>
        {
            s.Summary = "Get tenant performance metrics";
            s.Description = "Retrieves performance and usage metrics for the current tenant (tenant admin only)";
            s.Responses[200] = "Tenant metrics retrieved successfully";
            s.Responses[403] = "Forbidden - requires tenant admin privileges";
        });
        
        // Require tenant admin privileges
        Policies(TenantAuthorizationPolicies.TenantAdmin);
    }

    public override async Task HandleAsync(TenantMetricsRequest req, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Retrieving tenant metrics for tenant {TenantId}", _tenantContext.TenantId);

            // Get recent audit events for analysis
            var auditEvents = await _auditService.GetRecentAuditEventsAsync(
                _tenantContext.TenantId, 
                req.EventCount);

            // Calculate metrics
            var response = new TenantMetricsResponse
            {
                TenantId = _tenantContext.TenantId,
                TenantName = _tenantContext.TenantName,
                TenantSubdomain = _tenantContext.TenantSubdomain,
                GeneratedAt = startTime,
                Period = req.Period,
                Metrics = await CalculateMetricsAsync(auditEvents, req.Period, ct)
            };

            Response = response;

            _logger.LogDebug("Tenant metrics generated for {TenantId} in {Duration}ms",
                _tenantContext.TenantId, (DateTime.UtcNow - startTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant metrics for tenant {TenantId}", _tenantContext.TenantId);
            
            await SendErrorAsync(500, "Error retrieving tenant metrics", ct);
        }
    }

    private async Task<TenantMetrics> CalculateMetricsAsync(
        List<TenantAuditEvent> auditEvents,
        TimeSpan period,
        CancellationToken ct)
    {
        await Task.CompletedTask; // Synchronous calculations

        var cutoffTime = DateTime.UtcNow - period;
        var recentEvents = auditEvents.Where(e => e.Timestamp >= cutoffTime).ToList();

        var accessEvents = recentEvents.OfType<TenantAccessEvent>().ToList();
        var crossTenantEvents = recentEvents.OfType<CrossTenantAccessEvent>().ToList();
        var adminEvents = recentEvents.OfType<TenantAdminOperationEvent>().ToList();

        // Calculate access metrics
        var totalRequests = accessEvents.Count;
        var uniqueUsers = accessEvents.Where(e => e.UserId.HasValue).Select(e => e.UserId).Distinct().Count();
        var averageResponseTime = accessEvents.Any() ? accessEvents.Average(e => e.ResponseTimeMs) : 0;

        // Status code distribution
        var statusCodes = accessEvents
            .GroupBy(e => e.StatusCode)
            .ToDictionary(g => g.Key, g => g.Count());

        // Popular endpoints
        var popularEndpoints = accessEvents
            .GroupBy(e => e.Endpoint)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());

        // Error analysis
        var errorEvents = accessEvents.Where(e => e.StatusCode >= 400).ToList();
        var errorRate = totalRequests > 0 ? (double)errorEvents.Count / totalRequests : 0;

        // Performance analysis
        var slowRequests = accessEvents.Where(e => e.ResponseTimeMs > 1000).ToList();
        var slowRequestRate = totalRequests > 0 ? (double)slowRequests.Count / totalRequests : 0;

        // Security metrics
        var securityEvents = crossTenantEvents.Count;
        var blockedAttempts = crossTenantEvents.Count(e => e.WasBlocked);

        // Admin activity
        var adminOperations = adminEvents.Count;
        var successfulAdminOps = adminEvents.Count(e => e.Success);

        return new TenantMetrics
        {
            AccessMetrics = new AccessMetrics
            {
                TotalRequests = totalRequests,
                UniqueUsers = uniqueUsers,
                AverageResponseTimeMs = averageResponseTime,
                StatusCodeDistribution = statusCodes,
                PopularEndpoints = popularEndpoints,
                ErrorRate = errorRate,
                SlowRequestRate = slowRequestRate
            },
            SecurityMetrics = new SecurityMetrics
            {
                CrossTenantAttempts = securityEvents,
                BlockedAttempts = blockedAttempts,
                SecurityIncidentRate = totalRequests > 0 ? (double)securityEvents / totalRequests : 0
            },
            AdminMetrics = new AdminMetrics
            {
                TotalOperations = adminOperations,
                SuccessfulOperations = successfulAdminOps,
                SuccessRate = adminOperations > 0 ? (double)successfulAdminOps / adminOperations : 0
            },
            PerformanceMetrics = new PerformanceMetrics
            {
                TenantResolutionMetrics = new TenantResolutionPerformanceMetrics
                {
                    TotalAttempts = _resolutionMetrics.GetRecentMetrics().TotalRequests,
                    SuccessfulResolutions = _resolutionMetrics.GetRecentMetrics().SuccessCount,
                    AverageResolutionTimeMs = _resolutionMetrics.GetRecentMetrics().AverageDurationMs,
                    CacheHitRate = _resolutionMetrics.GetRecentMetrics().SuccessRate
                }
            }
        };
    }
}

/// <summary>
/// Request model for tenant metrics
/// </summary>
public sealed class TenantMetricsRequest
{
    /// <summary>
    /// Time period for metrics calculation (default: last 24 hours)
    /// </summary>
    public TimeSpan Period { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Maximum number of audit events to analyze (default: 10000)
    /// </summary>
    public int EventCount { get; set; } = 10000;
}

/// <summary>
/// Response model for tenant metrics
/// </summary>
public sealed class TenantMetricsResponse
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantSubdomain { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public TimeSpan Period { get; set; }
    public TenantMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Comprehensive tenant metrics
/// </summary>
public sealed class TenantMetrics
{
    public AccessMetrics AccessMetrics { get; set; } = new();
    public SecurityMetrics SecurityMetrics { get; set; } = new();
    public AdminMetrics AdminMetrics { get; set; } = new();
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Access-related metrics
/// </summary>
public sealed class AccessMetrics
{
    public int TotalRequests { get; set; }
    public int UniqueUsers { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public Dictionary<int, int> StatusCodeDistribution { get; set; } = new();
    public Dictionary<string, int> PopularEndpoints { get; set; } = new();
    public double ErrorRate { get; set; }
    public double SlowRequestRate { get; set; }
}

/// <summary>
/// Security-related metrics
/// </summary>
public sealed class SecurityMetrics
{
    public int CrossTenantAttempts { get; set; }
    public int BlockedAttempts { get; set; }
    public double SecurityIncidentRate { get; set; }
}

/// <summary>
/// Admin activity metrics
/// </summary>
public sealed class AdminMetrics
{
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Performance-related metrics
/// </summary>
public sealed class PerformanceMetrics
{
    public TenantResolutionPerformanceMetrics TenantResolutionMetrics { get; set; } = new();
}

/// <summary>
/// Tenant resolution performance metrics
/// </summary>
public sealed class TenantResolutionPerformanceMetrics
{
    public long TotalAttempts { get; set; }
    public long SuccessfulResolutions { get; set; }
    public double AverageResolutionTimeMs { get; set; }
    public double CacheHitRate { get; set; }
}