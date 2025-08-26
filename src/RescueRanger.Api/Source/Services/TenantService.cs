using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Data;
using RescueRanger.Api.Data.Repositories;
using RescueRanger.Api.Entities;
using RescueRanger.Api.Features.Admin;
using System.Text.RegularExpressions;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service implementation for tenant business operations
/// </summary>
public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TenantService> _logger;
    
    private static readonly Regex SubdomainRegex = new(@"^[a-z0-9-]+$", RegexOptions.Compiled);
    private static readonly string[] ReservedSubdomains = 
    [
        "admin", "api", "www", "mail", "ftp", "blog", "help", "support", "docs", "status", 
        "staging", "dev", "test", "demo", "app", "secure", "cdn", "static", "assets"
    ];

    public TenantService(ITenantRepository tenantRepository, ApplicationDbContext dbContext, ILogger<TenantService> logger)
    {
        _tenantRepository = tenantRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<TenantResponse>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new tenant with subdomain: {Subdomain}", request.Subdomain);

        // Validate subdomain
        var subdomainValidation = await ValidateSubdomainAsync(request.Subdomain, null, cancellationToken);
        if (!subdomainValidation.IsSuccess)
        {
            return Result<TenantResponse>.Error(string.Join("; ", subdomainValidation.Errors));
        }

        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Subdomain = request.Subdomain.ToLowerInvariant(),
                ContactEmail = request.ContactEmail,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                Status = TenantStatus.Provisioning,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System", // TODO: Get from current user context
                Configuration = new TenantConfiguration
                {
                    MaxHorses = request.Configuration?.MaxHorses ?? 100,
                    MaxUsers = request.Configuration?.MaxVolunteers ?? 50,
                    AdvancedFeaturesEnabled = false,
                    StorageLimitMb = 1024,
                    FeatureFlags = new Dictionary<string, bool>
                    {
                        ["EmailNotifications"] = request.Configuration?.EmailNotificationsEnabled ?? true,
                        ["SmsNotifications"] = request.Configuration?.SmsNotificationsEnabled ?? false
                    }
                }
            };

            // Generate API key
            tenant.RotateApiKey();

            var createResult = await _tenantRepository.CreateAsync(tenant);
            if (!createResult.IsSuccess)
            {
                _logger.LogError("Failed to create tenant: {Error}", createResult.Errors.First());
                return Result<TenantResponse>.Error(string.Join("; ", createResult.Errors));
            }

            // Start provisioning workflow
            var provisioningResult = await ProvisionTenantAsync(tenant.Id, cancellationToken);
            if (!provisioningResult.IsSuccess)
            {
                _logger.LogWarning("Tenant created but provisioning failed for {TenantId}: {Error}", 
                    tenant.Id, provisioningResult.Errors.First());
                // Don't fail the creation, but log the issue
            }

            var response = TenantResponseMapper.ToResponse(createResult.Value);
            _logger.LogInformation("Successfully created tenant {TenantId} with subdomain {Subdomain}", 
                tenant.Id, request.Subdomain);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant with subdomain {Subdomain}", request.Subdomain);
            return Result.Error("An error occurred while creating the tenant");
        }
    }

    public async Task<Result<TenantListResponse>> GetTenantsAsync(GetTenantsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _dbContext.Tenants.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.Status))
            {
                query = query.Where(t => t.Status == request.Status);
            }

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.ToLowerInvariant();
                query = query.Where(t => t.Name.ToLower().Contains(searchTerm) || 
                                        t.Subdomain.ToLower().Contains(searchTerm) ||
                                        t.ContactEmail.ToLower().Contains(searchTerm));
            }

            // Apply sorting
            query = request.SortBy?.ToLowerInvariant() switch
            {
                "name" => request.SortDirection.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(t => t.Name) 
                    : query.OrderBy(t => t.Name),
                "subdomain" => request.SortDirection.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(t => t.Subdomain) 
                    : query.OrderBy(t => t.Subdomain),
                "status" => request.SortDirection.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(t => t.Status) 
                    : query.OrderBy(t => t.Status),
                _ => request.SortDirection.ToLowerInvariant() == "desc" 
                    ? query.OrderByDescending(t => t.CreatedAt) 
                    : query.OrderBy(t => t.CreatedAt)
            };

            // Get total count for pagination
            var totalItems = await query.CountAsync(cancellationToken);

            // Apply pagination
            var pageSize = Math.Min(request.PageSize, 100); // Max 100 items per page
            var skip = (request.Page - 1) * pageSize;
            
            var tenants = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(t => new TenantSummaryResponse
                {
                    Id = t.Id,
                    Name = t.Name,
                    Subdomain = t.Subdomain,
                    ContactEmail = t.ContactEmail,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    ActivatedAt = t.ActivatedAt,
                    IsSystemTenant = t.IsSystemTenant,
                    IsActive = t.Status == TenantStatus.Active,
                    CanAccess = t.Status == TenantStatus.Active || t.Status == TenantStatus.Provisioning
                })
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var response = new TenantListResponse
            {
                Tenants = tenants,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = request.Page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasPreviousPage = request.Page > 1,
                    HasNextPage = request.Page < totalPages
                }
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants list");
            return Result.Error("An error occurred while retrieving tenants");
        }
    }

    public async Task<Result<TenantResponse>> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = await _tenantRepository.GetTenantEntityByIdAsync(tenantId);
        if (!result.IsSuccess)
        {
            return Result.NotFound($"Tenant with ID {tenantId} not found");
        }

        var response = TenantResponseMapper.ToResponse(result.Value);
        return Result.Success(response);
    }

    public async Task<TenantInfo?> GetTenantInfoAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = await _tenantRepository.GetTenantEntityByIdAsync(tenantId);
        if (!result.IsSuccess)
            return null;

        var tenant = result.Value;
        return new TenantInfo { Id = tenant.Id, Subdomain = tenant.Subdomain, Name = tenant.Name };
    }

    public async Task<Result<TenantResponse>> UpdateTenantAsync(Guid tenantId, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenantResult = await _tenantRepository.GetTenantEntityByIdAsync(tenantId);
        if (!tenantResult.IsSuccess)
        {
            return Result.NotFound($"Tenant with ID {tenantId} not found");
        }

        var tenant = tenantResult.Value;

        try
        {
            // Update basic properties
            if (!string.IsNullOrEmpty(request.Name))
            {
                tenant.Name = request.Name;
            }

            if (!string.IsNullOrEmpty(request.ContactEmail))
            {
                tenant.ContactEmail = request.ContactEmail;
            }

            if (request.PhoneNumber is not null)
            {
                tenant.PhoneNumber = request.PhoneNumber;
            }

            if (request.Address is not null)
            {
                tenant.Address = request.Address;
            }

            // Update configuration
            if (request.Configuration is not null)
            {
                if (request.Configuration.MaxHorses.HasValue)
                {
                    tenant.Configuration.MaxHorses = request.Configuration.MaxHorses.Value;
                }

                if (request.Configuration.MaxVolunteers.HasValue)
                {
                    tenant.Configuration.MaxUsers = request.Configuration.MaxVolunteers.Value;
                }

                if (request.Configuration.EmailNotificationsEnabled.HasValue)
                {
                    tenant.Configuration.FeatureFlags["EmailNotifications"] = request.Configuration.EmailNotificationsEnabled.Value;
                }

                if (request.Configuration.SmsNotificationsEnabled.HasValue)
                {
                    tenant.Configuration.FeatureFlags["SmsNotifications"] = request.Configuration.SmsNotificationsEnabled.Value;
                }

                if (request.Configuration.CustomSettings is not null)
                {
                    // Merge custom settings
                    foreach (var setting in request.Configuration.CustomSettings)
                    {
                        tenant.Configuration.Metadata[setting.Key] = setting.Value?.ToString() ?? "";
                    }
                }
            }

            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.UpdatedBy = "System"; // TODO: Get from current user context

            var updatedTenant = await _tenantRepository.UpdateAsync(tenant);
            var response = TenantResponseMapper.ToResponse(updatedTenant);

            _logger.LogInformation("Successfully updated tenant {TenantId}", tenantId);
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
            return Result.Error("An error occurred while updating the tenant");
        }
    }

    public async Task<Result<TenantResponse>> SuspendTenantAsync(Guid tenantId, SuspendTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenantResult = await _tenantRepository.GetTenantEntityByIdAsync(tenantId);
        if (!tenantResult.IsSuccess)
        {
            return Result.NotFound($"Tenant with ID {tenantId} not found");
        }

        var tenant = tenantResult.Value;

        if (tenant.Status == TenantStatus.Suspended)
        {
            return Result.Conflict("Tenant is already suspended");
        }

        if (tenant.IsSystemTenant)
        {
            return Result.Forbidden("System tenant cannot be suspended");
        }

        try
        {
            if (request.ImmediateSuspension)
            {
                tenant.Suspend(request.Reason);
            }
            else
            {
                // For scheduled suspension, you might want to implement a background job
                // For now, we'll just store the scheduled date in metadata
                tenant.Configuration.Metadata["ScheduledSuspensionAt"] = request.ScheduledSuspensionAt?.ToString("O") ?? "";
                tenant.Configuration.Metadata["SuspensionReason"] = request.Reason;
            }

            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.UpdatedBy = "System"; // TODO: Get from current user context

            var updatedTenant = await _tenantRepository.UpdateAsync(tenant);

            // TODO: Send notification to tenant if requested
            if (request.NotifyTenant)
            {
                _logger.LogInformation("Tenant suspension notification would be sent to {TenantId}", tenantId);
                // Implement notification logic here
            }

            var response = TenantResponseMapper.ToResponse(updatedTenant);
            _logger.LogInformation("Successfully suspended tenant {TenantId} with reason: {Reason}", tenantId, request.Reason);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant {TenantId}", tenantId);
            return Result.Error("An error occurred while suspending the tenant");
        }
    }

    public async Task<Result<TenantResponse>> ReactivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenantResult = await _tenantRepository.GetTenantEntityByIdAsync(tenantId);
        if (!tenantResult.IsSuccess)
        {
            return Result.NotFound($"Tenant with ID {tenantId} not found");
        }

        var tenant = tenantResult.Value;

        if (tenant.Status != TenantStatus.Suspended)
        {
            return Result.Conflict("Only suspended tenants can be reactivated");
        }

        try
        {
            tenant.Activate();
            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.UpdatedBy = "System"; // TODO: Get from current user context

            // Clear scheduled suspension metadata
            tenant.Configuration.Metadata.Remove("ScheduledSuspensionAt");
            tenant.Configuration.Metadata.Remove("SuspensionReason");

            var updatedTenant = await _tenantRepository.UpdateAsync(tenant);
            var response = TenantResponseMapper.ToResponse(updatedTenant);

            _logger.LogInformation("Successfully reactivated tenant {TenantId}", tenantId);
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating tenant {TenantId}", tenantId);
            return Result.Error("An error occurred while reactivating the tenant");
        }
    }

    public async Task<Result<bool>> ValidateSubdomainAsync(string subdomain, Guid? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        // Basic format validation
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            return Result.Error("Subdomain cannot be empty");
        }

        subdomain = subdomain.ToLowerInvariant().Trim();

        if (subdomain.Length < 3 || subdomain.Length > 50)
        {
            return Result.Error("Subdomain must be between 3 and 50 characters");
        }

        if (!SubdomainRegex.IsMatch(subdomain))
        {
            return Result.Error("Subdomain can only contain lowercase letters, numbers, and hyphens");
        }

        if (subdomain.StartsWith("-") || subdomain.EndsWith("-"))
        {
            return Result.Error("Subdomain cannot start or end with a hyphen");
        }

        // Check against reserved subdomains
        if (ReservedSubdomains.Contains(subdomain))
        {
            return Result.Error($"'{subdomain}' is a reserved subdomain and cannot be used");
        }

        // Check uniqueness
        var isAvailable = await _tenantRepository.IsSubdomainAvailableAsync(subdomain, excludeTenantId);
        if (!isAvailable)
        {
            return Result.Error($"Subdomain '{subdomain}' is already taken");
        }

        return Result.Success(true);
    }

    public async Task<Result> ProvisionTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting provisioning workflow for tenant {TenantId}", tenantId);

            // TODO: Implement actual provisioning logic
            // This might include:
            // - Creating database schema/tables for tenant
            // - Setting up storage containers
            // - Configuring monitoring and logging
            // - Sending welcome emails
            // - Creating default admin user

            // Simulate provisioning delay
            await Task.Delay(100, cancellationToken);

            // Update tenant status to Active
            var statusResult = await _tenantRepository.UpdateStatusAsync(tenantId, TenantStatus.Active);
            if (!statusResult.IsSuccess)
            {
                _logger.LogError("Failed to update tenant status to Active for {TenantId}: {Error}", 
                    tenantId, statusResult.Errors.First());
                return Result.Error("Failed to complete tenant provisioning");
            }

            _logger.LogInformation("Successfully completed provisioning for tenant {TenantId}", tenantId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning tenant {TenantId}", tenantId);
            return Result.Error("An error occurred during tenant provisioning");
        }
    }

    public async Task<Result<TenantResponse>> RotateApiKeyAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenantResult = await _tenantRepository.GetTenantEntityByIdAsync(tenantId);
        if (!tenantResult.IsSuccess)
        {
            return Result.NotFound($"Tenant with ID {tenantId} not found");
        }

        var tenant = tenantResult.Value;

        try
        {
            tenant.RotateApiKey();
            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.UpdatedBy = "System"; // TODO: Get from current user context

            var updatedTenant = await _tenantRepository.UpdateAsync(tenant);
            var response = TenantResponseMapper.ToResponse(updatedTenant);

            _logger.LogInformation("Successfully rotated API key for tenant {TenantId}", tenantId);
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rotating API key for tenant {TenantId}", tenantId);
            return Result.Error("An error occurred while rotating the API key");
        }
    }

    public async Task<Result> DeleteTenantAsync(Guid tenantId, string reason, CancellationToken cancellationToken = default)
    {
        var tenantResult = await _tenantRepository.GetTenantEntityByIdAsync(tenantId);
        if (!tenantResult.IsSuccess)
        {
            return Result.NotFound($"Tenant with ID {tenantId} not found");
        }

        var tenant = tenantResult.Value;

        if (tenant.IsSystemTenant)
        {
            return Result.Forbidden("System tenant cannot be deleted");
        }

        try
        {
            // Soft delete - mark as pending deletion
            var deleteResult = await _tenantRepository.DeleteAsync(tenantId);
            if (!deleteResult.IsSuccess)
            {
                return Result.Error(string.Join("; ", deleteResult.Errors));
            }

            _logger.LogInformation("Successfully marked tenant {TenantId} for deletion. Reason: {Reason}", tenantId, reason);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", tenantId);
            return Result.Error("An error occurred while deleting the tenant");
        }
    }

    public async Task<Result<TenantStatsResponse>> GetTenantStatsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenantResult = await _tenantRepository.GetTenantEntityByIdAsync(tenantId);
        if (!tenantResult.IsSuccess)
        {
            return Result.NotFound($"Tenant with ID {tenantId} not found");
        }

        try
        {
            // TODO: Implement actual stats calculation
            // This would query various tenant-specific tables to get counts
            
            var stats = new TenantStatsResponse
            {
                TotalHorses = await _dbContext.Horses.CountAsync(h => h.TenantId == tenantId, cancellationToken),
                TotalVolunteers = await _dbContext.Members.CountAsync(m => m.TenantId == tenantId, cancellationToken),
                ActiveCareRecords = 0, // TODO: Implement when care records exist
                StorageUsageBytes = 0, // TODO: Calculate storage usage
                ApiCallsThisMonth = 0, // TODO: Get from metrics/logging
                LastActivity = DateTime.UtcNow // TODO: Get actual last activity
            };

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for tenant {TenantId}", tenantId);
            return Result.Error("An error occurred while retrieving tenant statistics");
        }
    }
}