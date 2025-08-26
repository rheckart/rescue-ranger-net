using Ardalis.Result;
using RescueRanger.Api.Entities;
using RescueRanger.Api.Features.Admin;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service interface for tenant business operations
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Creates a new tenant with business logic and validation
    /// </summary>
    /// <param name="request">The tenant creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the created tenant or validation errors</returns>
    Task<Result<TenantResponse>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of tenants with filtering
    /// </summary>
    /// <param name="request">The query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the paginated tenant list</returns>
    Task<Result<TenantListResponse>> GetTenantsAsync(GetTenantsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the tenant details or not found error</returns>
    Task<Result<TenantResponse>> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets tenant info (lightweight) by ID
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>TenantInfo if found, null otherwise</returns>
    Task<TenantInfo?> GetTenantInfoAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates tenant configuration and settings
    /// </summary>
    /// <param name="tenantId">The tenant ID to update</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the updated tenant or validation errors</returns>
    Task<Result<TenantResponse>> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspends a tenant with reason and notification
    /// </summary>
    /// <param name="tenantId">The tenant ID to suspend</param>
    /// <param name="request">The suspension request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the updated tenant or validation errors</returns>
    Task<Result<TenantResponse>> SuspendTenantAsync(Guid tenantId, SuspendTenantRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a suspended tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID to reactivate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the reactivated tenant or validation errors</returns>
    Task<Result<TenantResponse>> ReactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates subdomain uniqueness and format
    /// </summary>
    /// <param name="subdomain">The subdomain to validate</param>
    /// <param name="excludeTenantId">Optional tenant ID to exclude from uniqueness check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating if subdomain is valid and available</returns>
    Task<Result<bool>> ValidateSubdomainAsync(string subdomain, Guid? excludeTenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs tenant provisioning workflow
    /// </summary>
    /// <param name="tenantId">The tenant ID to provision</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure of provisioning</returns>
    Task<Result> ProvisionTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the API key for a tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the updated tenant with new API key</returns>
    Task<Result<TenantResponse>> RotateApiKeyAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant (soft delete)
    /// </summary>
    /// <param name="tenantId">The tenant ID to delete</param>
    /// <param name="reason">Reason for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DeleteTenantAsync(Guid tenantId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tenant statistics and metrics
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing tenant statistics</returns>
    Task<Result<TenantStatsResponse>> GetTenantStatsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Response model for tenant statistics
/// </summary>
public class TenantStatsResponse
{
    /// <summary>
    /// Total number of horses in the system
    /// </summary>
    public int TotalHorses { get; set; }
    
    /// <summary>
    /// Total number of volunteers
    /// </summary>
    public int TotalVolunteers { get; set; }
    
    /// <summary>
    /// Number of active care records
    /// </summary>
    public int ActiveCareRecords { get; set; }
    
    /// <summary>
    /// Storage usage in bytes
    /// </summary>
    public long StorageUsageBytes { get; set; }
    
    /// <summary>
    /// API calls made this month
    /// </summary>
    public int ApiCallsThisMonth { get; set; }
    
    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime? LastActivity { get; set; }
}