using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Entities;
using RescueRanger.Api.Services;
using RescueRanger.Infrastructure.Data;

namespace RescueRanger.Api.Data.Repositories;

/// <summary>
/// Base repository implementation that automatically filters by tenant
/// </summary>
/// <typeparam name="T">Entity type that implements ITenantEntity</typeparam>
public class TenantAwareRepository<T> : ITenantAwareRepository<T> where T : BaseEntity, ITenantEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly ITenantContextService _tenantContext;
    protected readonly ITenantAuditService _auditService;
    protected readonly ILogger<TenantAwareRepository<T>> _logger;
    protected readonly DbSet<T> _dbSet;

    public TenantAwareRepository(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        ITenantAuditService auditService,
        ILogger<TenantAwareRepository<T>> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _auditService = auditService;
        _logger = logger;
        _dbSet = _context.Set<T>();
    }

    protected virtual IQueryable<T> GetTenantFilteredQuery()
    {
        if (!_tenantContext.IsValid)
        {
            throw new InvalidOperationException("Tenant context is not established");
        }

        // The global query filter in DbContext should handle tenant filtering,
        // but we can add an additional layer of protection here
        return _dbSet.Where(e => e.TenantId == _tenantContext.TenantId);
    }

    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery().ToListAsync(cancellationToken);
    }

    public virtual async Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery().Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        
        var entity = await GetTenantFilteredQuery().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        
        if (entity != null && !ValidateTenantOwnership(entity))
        {
            _logger.LogWarning("Cross-tenant access attempt blocked for entity {EntityType} {Id} in tenant {TenantId}",
                typeof(T).Name, id, _tenantContext.TenantId);
            return null;
        }

        return entity;
    }

    public virtual async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();

        // Ensure the entity belongs to the current tenant
        entity.TenantId = _tenantContext.TenantId;

        _dbSet.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Entity {EntityType} {Id} added to tenant {TenantId}",
            typeof(T).Name, entity.Id, _tenantContext.TenantId);

        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();

        if (!ValidateTenantOwnership(entity))
        {
            throw new UnauthorizedAccessException($"Entity {typeof(T).Name} {entity.Id} does not belong to tenant {_tenantContext.TenantId}");
        }

        // Ensure tenant ID cannot be changed
        entity.TenantId = _tenantContext.TenantId;

        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Entity {EntityType} {Id} updated in tenant {TenantId}",
            typeof(T).Name, entity.Id, _tenantContext.TenantId);

        return entity;
    }

    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();

        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        return await DeleteAsync(entity, cancellationToken);
    }

    public virtual async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();

        if (!ValidateTenantOwnership(entity))
        {
            throw new UnauthorizedAccessException($"Entity {typeof(T).Name} {entity.Id} does not belong to tenant {_tenantContext.TenantId}");
        }

        _dbSet.Remove(entity);
        var result = await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Entity {EntityType} {Id} deleted from tenant {TenantId}",
            typeof(T).Name, entity.Id, _tenantContext.TenantId);

        return result > 0;
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery().CountAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery().CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery().AnyAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery().AnyAsync(predicate, cancellationToken);
    }

    public virtual IQueryable<T> AsQueryable()
    {
        ValidateTenantContext();
        return GetTenantFilteredQuery();
    }

    public virtual async Task<bool> ValidateTenantOwnershipAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);
        return entity != null && ValidateTenantOwnership(entity);
    }

    public virtual bool ValidateTenantOwnership(T entity)
    {
        if (!_tenantContext.IsValid)
        {
            return false;
        }

        var belongsToTenant = entity.TenantId == _tenantContext.TenantId;
        
        if (!belongsToTenant)
        {
            _logger.LogWarning("Tenant ownership validation failed - Entity {EntityType} {Id} belongs to tenant {EntityTenantId} but current tenant is {CurrentTenantId}",
                typeof(T).Name, entity.Id, entity.TenantId, _tenantContext.TenantId);
        }

        return belongsToTenant;
    }

    protected void ValidateTenantContext()
    {
        if (!_tenantContext.IsValid)
        {
            throw new InvalidOperationException("Tenant context is not established. Ensure tenant resolution middleware is properly configured.");
        }
    }
}

/// <summary>
/// Concrete repository for horses with tenant awareness
/// </summary>
public class HorseRepository : TenantAwareRepository<Horse>
{
    public HorseRepository(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        ITenantAuditService auditService,
        ILogger<HorseRepository> logger) : base(context, tenantContext, auditService, logger)
    {
    }

    /// <summary>
    /// Gets horses by status for the current tenant
    /// </summary>
    public async Task<List<Horse>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery()
            .Where(h => h.Status == status)
            .OrderBy(h => h.Name)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Concrete repository for members with tenant awareness
/// </summary>
public class MemberRepository : TenantAwareRepository<Member>
{
    public MemberRepository(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        ITenantAuditService auditService,
        ILogger<MemberRepository> logger) : base(context, tenantContext, auditService, logger)
    {
    }

    /// <summary>
    /// Gets members by email for the current tenant
    /// </summary>
    public async Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ValidateTenantContext();
        return await GetTenantFilteredQuery()
            .FirstOrDefaultAsync(m => m.Email == email.ToLower(), cancellationToken);
    }
}