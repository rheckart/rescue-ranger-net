using System.Linq.Expressions;
using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Data.Repositories;

/// <summary>
/// Base interface for tenant-aware repositories
/// </summary>
/// <typeparam name="T">Entity type that implements ITenantEntity</typeparam>
public interface ITenantAwareRepository<T> where T : BaseEntity, ITenantEntity
{
    /// <summary>
    /// Gets all entities for the current tenant
    /// </summary>
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities matching a predicate for the current tenant
    /// </summary>
    Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity by ID for the current tenant
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching a predicate for the current tenant
    /// </summary>
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the current tenant
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity (validates tenant ownership)
    /// </summary>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by ID (validates tenant ownership)
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity (validates tenant ownership)
    /// </summary>
    Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a count of entities for the current tenant
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a count of entities matching a predicate for the current tenant
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entities exist for the current tenant
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entities matching a predicate exist for the current tenant
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a queryable for advanced operations (automatically filtered by tenant)
    /// </summary>
    IQueryable<T> AsQueryable();

    /// <summary>
    /// Validates that an entity belongs to the current tenant
    /// </summary>
    Task<bool> ValidateTenantOwnershipAsync(Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that an entity belongs to the current tenant
    /// </summary>
    bool ValidateTenantOwnership(T entity);
}