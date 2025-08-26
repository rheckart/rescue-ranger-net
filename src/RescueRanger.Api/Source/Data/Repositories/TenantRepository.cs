using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RescueRanger.Api.Data;
using System.Text.Json;
using Ardalis.Result;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Data.Repositories;

/// <summary>
/// Repository implementation for tenant operations
/// </summary>
public class TenantRepository(
    ApplicationDbContext context,
    IDistributedCache cache,
    ILogger<TenantRepository> logger)
    : ITenantRepository
{
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private const string CacheKeyPrefix = "tenant:";

    /// <inheritdoc />
    public async Task<Result<TenantInfo>> GetBySubdomainAsync(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return Result.Invalid();
        
        var cacheKey = $"{CacheKeyPrefix}subdomain:{subdomain.ToLowerInvariant()}";
        
        // Try cache first
        var cachedResult = await GetFromCacheAsync<TenantInfo>(cacheKey);
        if (cachedResult.IsSuccess)
            return Result.Success(cachedResult);
        
        // Query database
        var tenant = await context.AllTenants<Tenant>()
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
        
        if (tenant is not null)
        {
            await SetCacheAsync(cacheKey, tenant);
            logger.LogDebug("Tenant found by subdomain: {Subdomain} -> {TenantId}", subdomain, tenant.Id);
            return Result.Success(tenant);
        }

        logger.LogDebug("Tenant not found by subdomain: {Subdomain}", subdomain);
        return Result.NotFound();
    }
    
    /// <inheritdoc />
    public async Task<Result<TenantInfo>> GetByIdAsync(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            return Result.Invalid();
        
        var cacheKey = $"{CacheKeyPrefix}id:{tenantId}";
        
        // Try cache first
        var cachedResult = await GetFromCacheAsync<TenantInfo>(cacheKey);
        if (cachedResult.IsSuccess)
            return Result.Success(cachedResult);
        
        // Query database
        var tenant = await context.AllTenants<Tenant>()
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
        
        if (tenant is not null)
        {
            await SetCacheAsync(cacheKey, tenant);
            logger.LogDebug("Tenant found by ID: {TenantId}", tenantId);
            return Result.Success(tenant);
        }

        logger.LogDebug("Tenant not found by ID: {TenantId}", tenantId);
        return Result.NotFound();
    }
    
    /// <inheritdoc />
    public async Task<Result<Tenant>> GetTenantEntityByIdAsync(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            return Result.Invalid();
        
        var tenant = await context.AllTenants<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        return tenant is not null ? Result.Success(tenant) : Result.NotFound();
    }
    
    /// <inheritdoc />
    public async Task<Tenant?> GetTenantEntityBySubdomainAsync(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return null;
        
        return await context.AllTenants<Tenant>()
            .FirstOrDefaultAsync(t => t.Subdomain.ToLower() == subdomain.ToLowerInvariant());
    }
    
    /// <inheritdoc />
    public async Task<Result<Tenant>> CreateAsync(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        
        // Validate subdomain uniqueness
        var existingTenant = await GetTenantEntityBySubdomainAsync(tenant.Subdomain);
        if (existingTenant is not null)
        {
            return Result<Tenant>.Unavailable();
        }
        
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        
        // Invalidate cache
        await InvalidateTenantCacheAsync(tenant.Id, tenant.Subdomain);
        
        logger.LogInformation("Tenant created: {TenantId} ({Subdomain})", tenant.Id, tenant.Subdomain);
        return Result.Created(tenant);
    }
    
    /// <inheritdoc />
    public async Task<Tenant> UpdateAsync(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        
        context.Tenants.Update(tenant);
        await context.SaveChangesAsync();
        
        // Invalidate cache
        await InvalidateTenantCacheAsync(tenant.Id, tenant.Subdomain);
        
        logger.LogInformation("Tenant updated: {TenantId} ({Subdomain})", tenant.Id, tenant.Subdomain);
        return tenant;
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<TenantInfo>> GetAllAsync(
        string? status = null, 
        int pageNumber = 1, 
        int pageSize = 50)
    {
        var query = context.AllTenants<Tenant>().AsQueryable();
        
        if (status.HasValue())
        {
            query = query.Where(t => t.Status == status);
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
        
        var query = context.AllTenants<Tenant>()
            .Where(t => t.Subdomain.ToLower() == subdomain.ToLowerInvariant());
        
        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTenantId.Value);
        }
        
        return !await query.AnyAsync();
    }
    
    /// <inheritdoc />
    public async Task<Result<bool>> ValidateTenantStatusAsync(Guid tenantId)
    {
        var tenantResult = await GetByIdAsync(tenantId);
        if (tenantResult.IsNotFound()) return Result.NotFound();
        return tenantResult.Value.CanAccess;
    }
    
    /// <inheritdoc />
    public async Task<Result> UpdateStatusAsync(Guid tenantId, string status, string? reason = null)
    {
        var tenantResult = await GetTenantEntityByIdAsync(tenantId);
        if (tenantResult.IsNotFound())
            return Result.NotFound();
        
        var tenant = tenantResult.Value;
        
        var oldStatus = tenantResult.Value.Status;
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

        context.Attach(tenant);
        await context.SaveChangesAsync();
        await InvalidateTenantCacheAsync(tenant.Id, tenant.Subdomain);
        
        logger.LogInformation("Tenant status updated: {TenantId} ({Subdomain}) from {OldStatus} to {NewStatus}",
            tenant.Id, tenant.Subdomain, oldStatus, status);
        
        return Result.Success();
    }
    
    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Guid tenantId)
    {
        // For now, we'll use soft delete by marking as PendingDeletion
        return await UpdateStatusAsync(tenantId, TenantStatus.PendingDeletion, "Tenant deleted");
    }
    
    private async Task<Result<T>> GetFromCacheAsync<T>(string cacheKey) where T : class
    {
        try
        {
            var cached = await cache.GetStringAsync(cacheKey);
            if (cached is not null)
            {
                var deserialized = JsonSerializer.Deserialize<T>(cached);
                return Result.Success(deserialized!);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get from cache: {CacheKey}", cacheKey);
        }
        
        return Result.NotFound();
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
            
            await cache.SetStringAsync(cacheKey, serialized, options);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to set cache: {CacheKey}", cacheKey);
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
                await cache.RemoveAsync(key);
            }
            
            logger.LogDebug("Tenant cache invalidated: {TenantId} ({Subdomain})", tenantId, subdomain);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to invalidate tenant cache: {TenantId} ({Subdomain})", tenantId, subdomain);
        }
    }
    
    /// <inheritdoc />
    public async Task<int> GetActiveTenantsCountAsync()
    {
        return await context.Tenants
            .Where(t => t.Status == TenantStatus.Active)
            .CountAsync();
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
    {
        return await context.AllTenants<Tenant>().ToListAsync();
    }
}