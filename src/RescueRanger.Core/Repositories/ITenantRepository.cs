using RescueRanger.Core.Entities;
using RescueRanger.Core.Enums;
using RescueRanger.Core.Models;

namespace RescueRanger.Core.Repositories;

/// <summary>
/// Repository interface for tenant operations
/// </summary>
public interface ITenantRepository
{
    /// <summary>
    /// Gets a tenant by subdomain
    /// </summary>
    /// <param name="subdomain">The tenant subdomain</param>
    /// <returns>TenantInfo if found, null otherwise</returns>
    Task<TenantInfo?> GetBySubdomainAsync(string subdomain);
    
    /// <summary>
    /// Gets a tenant by ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>TenantInfo if found, null otherwise</returns>
    Task<TenantInfo?> GetByIdAsync(Guid tenantId);
    
    /// <summary>
    /// Gets the full tenant entity by ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Tenant entity if found, null otherwise</returns>
    Task<Tenant?> GetTenantEntityByIdAsync(Guid tenantId);
    
    /// <summary>
    /// Gets the full tenant entity by subdomain
    /// </summary>
    /// <param name="subdomain">The tenant subdomain</param>
    /// <returns>Tenant entity if found, null otherwise</returns>
    Task<Tenant?> GetTenantEntityBySubdomainAsync(string subdomain);
    
    /// <summary>
    /// Creates a new tenant
    /// </summary>
    /// <param name="tenant">The tenant to create</param>
    /// <returns>The created tenant</returns>
    Task<Tenant> CreateAsync(Tenant tenant);
    
    /// <summary>
    /// Updates an existing tenant
    /// </summary>
    /// <param name="tenant">The tenant to update</param>
    /// <returns>The updated tenant</returns>
    Task<Tenant> UpdateAsync(Tenant tenant);
    
    /// <summary>
    /// Gets all tenants with optional filtering
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <returns>List of tenants</returns>
    Task<IEnumerable<TenantInfo>> GetAllAsync(
        TenantStatus? status = null, 
        int pageNumber = 1, 
        int pageSize = 50);
    
    /// <summary>
    /// Checks if a subdomain is available
    /// </summary>
    /// <param name="subdomain">The subdomain to check</param>
    /// <param name="excludeTenantId">Optional tenant ID to exclude from the check</param>
    /// <returns>True if available, false otherwise</returns>
    Task<bool> IsSubdomainAvailableAsync(string subdomain, Guid? excludeTenantId = null);
    
    /// <summary>
    /// Validates tenant status and access
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>True if tenant is valid and can be accessed</returns>
    Task<bool> ValidateTenantStatusAsync(Guid tenantId);
    
    /// <summary>
    /// Updates tenant status
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="status">The new status</param>
    /// <param name="reason">Optional reason for status change</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateStatusAsync(Guid tenantId, TenantStatus status, string? reason = null);
    
    /// <summary>
    /// Deletes a tenant (soft delete)
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteAsync(Guid tenantId);
}