using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using RescueRanger.Core.Entities;
using RescueRanger.Core.Enums;
using RescueRanger.Core.Models;
using RescueRanger.Core.Repositories;
using RescueRanger.Infrastructure.Data;
using System.Text.Json;

namespace RescueRanger.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for tenant operations
/// </summary>
public class TenantRepository : ITenantRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TenantRepository> _logger;
    
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private const string CacheKeyPrefix = "tenant:";
    
    public TenantRepository(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<TenantRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<TenantInfo?> GetBySubdomainAsync(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return null;
        
        var cacheKey = $"{CacheKeyPrefix}subdomain:{subdomain.ToLowerInvariant()}";
        
        // Try cache first
        var cached = await GetFromCacheAsync<TenantInfo>(cacheKey);
        if (cached != null)
            return cached;
        
        // Query database
        var tenant = await _context.AllTenants<Tenant>()
            .Where(t => t.Subdomain.ToLower() == subdomain.ToLowerInvariant())
            .Select(t => new TenantInfo
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Status = t.Status,
                Configuration = t.Configuration,
                IsSystemTenant = t.IsSystemTenant,
                CreatedAt = t.CreatedAt,
                ActivatedAt = t.ActivatedAt,
                SuspendedAt = t.SuspendedAt,
                SuspensionReason = t.SuspensionReason
            })
            .FirstOrDefaultAsync();
        
        if (tenant != null)
        {
            await SetCacheAsync(cacheKey, tenant);
            _logger.LogDebug("Tenant found by subdomain: {Subdomain} -> {TenantId}", subdomain, tenant.Id);
        }
        else
        {
            _logger.LogDebug("Tenant not found by subdomain: {Subdomain}", subdomain);
        }
        
        return tenant;
    }
    
    /// <inheritdoc />
    public async Task<TenantInfo?> GetByIdAsync(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            return null;
        
        var cacheKey = $"{CacheKeyPrefix}id:{tenantId}";
        
        // Try cache first
        var cached = await GetFromCacheAsync<TenantInfo>(cacheKey);
        if (cached != null)
            return cached;
        
        // Query database
        var tenant = await _context.AllTenants<Tenant>()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantInfo
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Status = t.Status,
                Configuration = t.Configuration,
                IsSystemTenant = t.IsSystemTenant,
                CreatedAt = t.CreatedAt,
                ActivatedAt = t.ActivatedAt,
                SuspendedAt = t.SuspendedAt,
                SuspensionReason = t.SuspensionReason
            })
            .FirstOrDefaultAsync();
        
        if (tenant != null)
        {
            await SetCacheAsync(cacheKey, tenant);
            _logger.LogDebug("Tenant found by ID: {TenantId}", tenantId);
        }
        else
        {
            _logger.LogDebug("Tenant not found by ID: {TenantId}", tenantId);
        }
        
        return tenant;
    }
    
    /// <inheritdoc />
    public async Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            return null;
        
        return await _context.AllTenants<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId);
    }
    
    /// <inheritdoc />
    public async Task<Tenant?> GetTenantEntityBySubdomainAsync(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return null;
        
        return await _context.AllTenants<Tenant>()
            .FirstOrDefaultAsync(t => t.Subdomain.ToLower() == subdomain.ToLowerInvariant());
    }
    
    /// <inheritdoc />
    public async Task<Tenant> CreateAsync(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        
        // Validate subdomain uniqueness
        var existingTenant = await GetTenantEntityBySubdomainAsync(tenant.Subdomain);
        if (existingTenant != null)
        {
            throw new InvalidOperationException($"Subdomain '{tenant.Subdomain}' is already in use");
        }
        
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        
        // Invalidate cache
        await InvalidateTenantCacheAsync(tenant.Id, tenant.Subdomain);
        
        _logger.LogInformation("Tenant created: {TenantId} ({Subdomain})", tenant.Id, tenant.Subdomain);
        return tenant;
    }
    
    /// <inheritdoc />
    public async Task<Tenant> UpdateAsync(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        
        _context.Tenants.Update(tenant);
        await _context.SaveChangesAsync();
        
        // Invalidate cache
        await InvalidateTenantCacheAsync(tenant.Id, tenant.Subdomain);
        
        _logger.LogInformation("Tenant updated: {TenantId} ({Subdomain})", tenant.Id, tenant.Subdomain);
        return tenant;
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<TenantInfo>> GetAllAsync(
        TenantStatus? status = null, 
        int pageNumber = 1, 
        int pageSize = 50)
    {
        var query = _context.AllTenants<Tenant>().AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }
        
        var tenants = await query
            .OrderBy(t => t.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TenantInfo
            {
                Id = t.Id,
                Name = t.Name,
                Subdomain = t.Subdomain,
                Status = t.Status,
                Configuration = t.Configuration,
                IsSystemTenant = t.IsSystemTenant,
                CreatedAt = t.CreatedAt,
                ActivatedAt = t.ActivatedAt,
                SuspendedAt = t.SuspendedAt,
                SuspensionReason = t.SuspensionReason
            })
            .ToListAsync();
        
        return tenants;
    }
    
    /// <inheritdoc />
    public async Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTenantId = null)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;
        
        var query = _context.AllTenants<Tenant>()
            .Where(t => t.Subdomain.ToLower() == subdomain.ToLowerInvariant());
        
        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTenantId.Value);
        }
        
        return !await query.AnyAsync();
    }
    
    /// <inheritdoc />
    public async Task<bool> ValidateTenantStatusAsync(Guid tenantId)
    {
        var tenant = await GetByIdAsync(tenantId);
        return tenant?.CanAccess == true;
    }
    
    /// <inheritdoc />
    public async Task<bool> UpdateStatusAsync(Guid tenantId, TenantStatus status, string? reason = null)
    {
        var tenant = await GetTenantEntityByIdAsync(tenantId);
        if (tenant == null)
            return false;
        
        var oldStatus = tenant.Status;
        tenant.Status = status;
        
        switch (status)
        {
            case TenantStatus.Active:
                tenant.Activate();
                break;
            case TenantStatus.Suspended:
                tenant.Suspend(reason ?? "No reason provided");
                break;
            case TenantStatus.PendingDeletion:
                tenant.SuspendedAt = DateTime.UtcNow;
                tenant.SuspensionReason = reason ?? "Marked for deletion";
                break;
        }
        
        await _context.SaveChangesAsync();
        await InvalidateTenantCacheAsync(tenant.Id, tenant.Subdomain);
        
        _logger.LogInformation("Tenant status updated: {TenantId} ({Subdomain}) from {OldStatus} to {NewStatus}",
            tenant.Id, tenant.Subdomain, oldStatus, status);
        
        return true;
    }
    
    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid tenantId)
    {
        // For now, we'll use soft delete by marking as PendingDeletion
        return await UpdateStatusAsync(tenantId, TenantStatus.PendingDeletion, "Tenant deleted");
    }
    
    private async Task<T?> GetFromCacheAsync<T>(string cacheKey) where T : class
    {
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<T>(cached);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get from cache: {CacheKey}", cacheKey);
        }
        
        return null;
    }
    
    private async Task SetCacheAsync<T>(string cacheKey, T value) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _cacheExpiration
            };
            
            await _cache.SetStringAsync(cacheKey, serialized, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache: {CacheKey}", cacheKey);
        }
    }
    
    private async Task InvalidateTenantCacheAsync(Guid tenantId, string subdomain)
    {
        try
        {
            var keys = new[]
            {
                $"{CacheKeyPrefix}id:{tenantId}",
                $"{CacheKeyPrefix}subdomain:{subdomain.ToLowerInvariant()}"
            };
            
            foreach (var key in keys)
            {
                await _cache.RemoveAsync(key);
            }
            
            _logger.LogDebug("Tenant cache invalidated: {TenantId} ({Subdomain})", tenantId, subdomain);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate tenant cache: {TenantId} ({Subdomain})", tenantId, subdomain);
        }
    }
}