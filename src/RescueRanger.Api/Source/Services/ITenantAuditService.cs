namespace RescueRanger.Api.Services;

/// <summary>
/// Service for auditing tenant-related activities
/// </summary>
public interface ITenantAuditService
{
    /// <summary>
    /// Logs a tenant access event
    /// </summary>
    Task LogTenantAccessAsync(TenantAccessEvent accessEvent);

    /// <summary>
    /// Logs a cross-tenant access attempt
    /// </summary>
    Task LogCrossTenantAccessAttemptAsync(CrossTenantAccessEvent accessEvent);

    /// <summary>
    /// Logs a tenant admin operation
    /// </summary>
    Task LogTenantAdminOperationAsync(TenantAdminOperationEvent operationEvent);

    /// <summary>
    /// Gets recent audit events for a tenant
    /// </summary>
    Task<List<TenantAuditEvent>> GetRecentAuditEventsAsync(Guid tenantId, int count = 100);

    /// <summary>
    /// Gets audit events for a specific user
    /// </summary>
    Task<List<TenantAuditEvent>> GetUserAuditEventsAsync(Guid userId, Guid? tenantId = null, int count = 100);
}

/// <summary>
/// Base class for tenant audit events
/// </summary>
public abstract class TenantAuditEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid TenantId { get; init; }
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string RequestId { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string UserAgent { get; init; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}

/// <summary>
/// Event for successful tenant access
/// </summary>
public class TenantAccessEvent : TenantAuditEvent
{
    public TenantAccessEvent()
    {
        EventType = "TenantAccess";
    }

    public string Endpoint { get; init; } = string.Empty;
    public string HttpMethod { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public long ResponseTimeMs { get; init; }
}

/// <summary>
/// Event for cross-tenant access attempts
/// </summary>
public class CrossTenantAccessEvent : TenantAuditEvent
{
    public CrossTenantAccessEvent()
    {
        EventType = "CrossTenantAccessAttempt";
    }

    public Guid TargetTenantId { get; init; }
    public string TargetTenantName { get; init; } = string.Empty;
    public string AttemptedEndpoint { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public bool WasBlocked { get; init; } = true;
}

/// <summary>
/// Event for tenant admin operations
/// </summary>
public class TenantAdminOperationEvent : TenantAuditEvent
{
    public TenantAdminOperationEvent()
    {
        EventType = "TenantAdminOperation";
    }

    public string Operation { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public Guid? ResourceId { get; init; }
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}