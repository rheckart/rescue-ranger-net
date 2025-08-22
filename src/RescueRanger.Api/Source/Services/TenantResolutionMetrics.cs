using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for tracking tenant resolution metrics
/// </summary>
public class TenantResolutionMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _successCounter;
    private readonly Counter<long> _failureCounter;
    private readonly Histogram<double> _durationHistogram;
    private readonly UpDownCounter<int> _activeTenantCounter;
    
    // In-memory tracking for recent metrics (last 5 minutes)
    private readonly ConcurrentDictionary<string, MetricSnapshot> _recentMetrics = new();
    private readonly Timer _cleanupTimer;
    
    public TenantResolutionMetrics()
    {
        _meter = new Meter("RescueRanger.TenantResolution", "1.0");
        
        _successCounter = _meter.CreateCounter<long>(
            "tenant_resolution_success_total",
            description: "Total number of successful tenant resolutions");
        
        _failureCounter = _meter.CreateCounter<long>(
            "tenant_resolution_failure_total", 
            description: "Total number of failed tenant resolutions");
        
        _durationHistogram = _meter.CreateHistogram<double>(
            "tenant_resolution_duration_ms",
            unit: "milliseconds",
            description: "Duration of tenant resolution in milliseconds");
        
        _activeTenantCounter = _meter.CreateUpDownCounter<int>(
            "active_tenants",
            description: "Number of currently active tenants");
        
        // Cleanup old metrics every minute
        _cleanupTimer = new Timer(_ => CleanupOldMetrics(), null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    /// <summary>
    /// Records a successful tenant resolution
    /// </summary>
    public void RecordSuccess(string method, double durationMs, string? tenantId = null)
    {
        _successCounter.Add(1, 
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("tenant_id", tenantId));
        
        _durationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("result", "success"));
        
        UpdateRecentMetrics(method, true, durationMs);
    }
    
    /// <summary>
    /// Records a failed tenant resolution
    /// </summary>
    public void RecordFailure(string reason, double durationMs, string? method = null)
    {
        _failureCounter.Add(1,
            new KeyValuePair<string, object?>("reason", reason),
            new KeyValuePair<string, object?>("method", method ?? "unknown"));
        
        _durationHistogram.Record(durationMs,
            new KeyValuePair<string, object?>("method", method ?? "unknown"),
            new KeyValuePair<string, object?>("result", "failure"));
        
        UpdateRecentMetrics(method ?? "unknown", false, durationMs);
    }
    
    /// <summary>
    /// Updates the active tenant count
    /// </summary>
    public void UpdateActiveTenantCount(int count)
    {
        _activeTenantCounter.Add(count);
    }
    
    /// <summary>
    /// Gets recent metrics summary
    /// </summary>
    public MetricsSummary GetRecentMetrics()
    {
        var now = DateTime.UtcNow;
        var fiveMinutesAgo = now.AddMinutes(-5);
        
        var recentSnapshots = _recentMetrics.Values
            .Where(s => s.Timestamp > fiveMinutesAgo)
            .ToList();
        
        if (!recentSnapshots.Any())
        {
            return new MetricsSummary();
        }
        
        var successCount = recentSnapshots.Count(s => s.Success);
        var failureCount = recentSnapshots.Count(s => !s.Success);
        var totalCount = successCount + failureCount;
        
        return new MetricsSummary
        {
            TotalRequests = totalCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            SuccessRate = totalCount > 0 ? (double)successCount / totalCount * 100 : 0,
            AverageDurationMs = recentSnapshots.Average(s => s.DurationMs),
            MaxDurationMs = recentSnapshots.Max(s => s.DurationMs),
            MinDurationMs = recentSnapshots.Min(s => s.DurationMs),
            MethodBreakdown = recentSnapshots
                .GroupBy(s => s.Method)
                .ToDictionary(
                    g => g.Key,
                    g => new MethodMetrics
                    {
                        Count = g.Count(),
                        SuccessRate = g.Count() > 0 ? 
                            (double)g.Count(s => s.Success) / g.Count() * 100 : 0,
                        AverageDurationMs = g.Average(s => s.DurationMs)
                    })
        };
    }
    
    private void UpdateRecentMetrics(string method, bool success, double durationMs)
    {
        var key = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid()}";
        _recentMetrics.TryAdd(key, new MetricSnapshot
        {
            Timestamp = DateTime.UtcNow,
            Method = method,
            Success = success,
            DurationMs = durationMs
        });
    }
    
    private void CleanupOldMetrics()
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        var keysToRemove = _recentMetrics
            .Where(kvp => kvp.Value.Timestamp < fiveMinutesAgo)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in keysToRemove)
        {
            _recentMetrics.TryRemove(key, out _);
        }
    }
    
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _meter?.Dispose();
    }
    
    private class MetricSnapshot
    {
        public DateTime Timestamp { get; init; }
        public string Method { get; init; } = string.Empty;
        public bool Success { get; init; }
        public double DurationMs { get; init; }
    }
}

/// <summary>
/// Summary of recent tenant resolution metrics
/// </summary>
public class MetricsSummary
{
    public int TotalRequests { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public double SuccessRate { get; init; }
    public double AverageDurationMs { get; init; }
    public double MaxDurationMs { get; init; }
    public double MinDurationMs { get; init; }
    public Dictionary<string, MethodMetrics> MethodBreakdown { get; init; } = new();
}

/// <summary>
/// Metrics for a specific resolution method
/// </summary>
public class MethodMetrics
{
    public int Count { get; init; }
    public double SuccessRate { get; init; }
    public double AverageDurationMs { get; init; }
}