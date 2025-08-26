using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using RescueRanger.Infrastructure.Data;

namespace RescueRanger.Api.Services;

/// <summary>
/// Implementation of tenant audit service
/// </summary>
public class TenantAuditService : ITenantAuditService
{
    private readonly ILogger<TenantAuditService> _logger;
    private readonly IDistributedCache _cache;
    private readonly ApplicationDbContext _dbContext;

    public TenantAuditService(
        ILogger<TenantAuditService> logger,
        IDistributedCache cache,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _cache = cache;
        _dbContext = dbContext;
    }

    public async Task LogTenantAccessAsync(TenantAccessEvent accessEvent)
    {
        try
        {
            // Log to structured logging
            _logger.LogInformation("Tenant access: {TenantId} - {UserEmail} - {Endpoint} - {StatusCode} - {ResponseTime}ms",
                accessEvent.TenantId, accessEvent.UserEmail, accessEvent.Endpoint, 
                accessEvent.StatusCode, accessEvent.ResponseTimeMs);

            // Store in cache for quick access to recent events
            var cacheKey = $"tenant_audit:{accessEvent.TenantId}:recent";
            await AddEventToCache(cacheKey, accessEvent);

            // Store user-specific events
            if (accessEvent.UserId.HasValue)
            {
                var userCacheKey = $"user_audit:{accessEvent.UserId}:recent";
                await AddEventToCache(userCacheKey, accessEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging tenant access event");
        }
    }

    public async Task LogCrossTenantAccessAttemptAsync(CrossTenantAccessEvent accessEvent)
    {
        try
        {
            // Log security event with high priority
            _logger.LogWarning("Cross-tenant access attempt: User {UserEmail} from tenant {TenantId} attempted to access tenant {TargetTenantId} - {Endpoint} - Blocked: {WasBlocked}",
                accessEvent.UserEmail, accessEvent.TenantId, accessEvent.TargetTenantId, 
                accessEvent.AttemptedEndpoint, accessEvent.WasBlocked);

            // Store security events with longer retention
            var securityCacheKey = $"security_audit:{accessEvent.TenantId}:cross_tenant";
            await AddEventToCache(securityCacheKey, accessEvent, TimeSpan.FromDays(7));

            // Global security events for system monitoring
            var globalSecurityKey = "security_audit:global:cross_tenant";
            await AddEventToCache(globalSecurityKey, accessEvent, TimeSpan.FromDays(30));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging cross-tenant access attempt");
        }
    }

    public async Task LogTenantAdminOperationAsync(TenantAdminOperationEvent operationEvent)
    {
        try
        {
            // Log admin operation
            _logger.LogInformation("Tenant admin operation: {TenantId} - {UserEmail} - {Operation} - {ResourceType}:{ResourceId} - Success: {Success}",
                operationEvent.TenantId, operationEvent.UserEmail, operationEvent.Operation,
                operationEvent.ResourceType, operationEvent.ResourceId, operationEvent.Success);

            // Store admin events
            var adminCacheKey = $"tenant_audit:{operationEvent.TenantId}:admin_ops";
            await AddEventToCache(adminCacheKey, operationEvent, TimeSpan.FromDays(30));

            // Store user admin events
            if (operationEvent.UserId.HasValue)
            {
                var userAdminKey = $"user_audit:{operationEvent.UserId}:admin_ops";
                await AddEventToCache(userAdminKey, operationEvent, TimeSpan.FromDays(30));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging tenant admin operation");
        }
    }

    public async Task<List<TenantAuditEvent>> GetRecentAuditEventsAsync(Guid tenantId, int count = 100)
    {
        var events = new List<TenantAuditEvent>();

        try
        {
            var cacheKey = $"tenant_audit:{tenantId}:recent";
            var cachedEvents = await GetEventsFromCache(cacheKey);
            events.AddRange(cachedEvents);

            // Also get admin operations
            var adminCacheKey = $"tenant_audit:{tenantId}:admin_ops";
            var adminEvents = await GetEventsFromCache(adminCacheKey);
            events.AddRange(adminEvents);

            // Sort by timestamp and limit
            events = events
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events for tenant {TenantId}", tenantId);
        }

        return events;
    }

    public async Task<List<TenantAuditEvent>> GetUserAuditEventsAsync(Guid userId, Guid? tenantId = null, int count = 100)
    {
        var events = new List<TenantAuditEvent>();

        try
        {
            var cacheKey = $"user_audit:{userId}:recent";
            var cachedEvents = await GetEventsFromCache(cacheKey);
            
            // Filter by tenant if specified
            if (tenantId.HasValue)
            {
                cachedEvents = cachedEvents.Where(e => e.TenantId == tenantId.Value).ToList();
            }
            
            events.AddRange(cachedEvents);

            // Get admin operations
            var adminCacheKey = $"user_audit:{userId}:admin_ops";
            var adminEvents = await GetEventsFromCache(adminCacheKey);
            
            if (tenantId.HasValue)
            {
                adminEvents = adminEvents.Where(e => e.TenantId == tenantId.Value).ToList();
            }
            
            events.AddRange(adminEvents);

            // Sort and limit
            events = events
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit events for user {UserId}", userId);
        }

        return events;
    }

    private async Task AddEventToCache(string cacheKey, TenantAuditEvent auditEvent, TimeSpan? expiration = null)
    {
        try
        {
            var existingEvents = await GetEventsFromCache(cacheKey);
            existingEvents.Add(auditEvent);

            // Keep only the most recent 1000 events
            if (existingEvents.Count > 1000)
            {
                existingEvents = existingEvents
                    .OrderByDescending(e => e.Timestamp)
                    .Take(1000)
                    .ToList();
            }

            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            }

            var serializedEvents = JsonSerializer.Serialize(existingEvents);
            await _cache.SetStringAsync(cacheKey, serializedEvents, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding event to cache with key {CacheKey}", cacheKey);
        }
    }

    private async Task<List<TenantAuditEvent>> GetEventsFromCache(string cacheKey)
    {
        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var events = JsonSerializer.Deserialize<List<TenantAuditEvent>>(cachedData);
                return events ?? new List<TenantAuditEvent>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events from cache with key {CacheKey}", cacheKey);
        }

        return new List<TenantAuditEvent>();
    }
}