using Microsoft.Extensions.Logging;
using RescueRanger.Core.Models;
using RescueRanger.Core.Services;
using RescueRanger.Core.ValueObjects;

namespace RescueRanger.Infrastructure.Services;

/// <summary>
/// Implementation of ITenantContextService for managing tenant context
/// </summary>
public class TenantContextService : ITenantContextService
{
    private readonly ILogger<TenantContextService> _logger;
    private TenantInfo? _currentTenant;
    
    public TenantContextService(ILogger<TenantContextService> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc />
    public Guid TenantId => _currentTenant?.Id ?? Guid.Empty;
    
    /// <inheritdoc />
    public string TenantSubdomain => _currentTenant?.Subdomain ?? string.Empty;
    
    /// <inheritdoc />
    public string TenantName => _currentTenant?.Name ?? string.Empty;
    
    /// <inheritdoc />
    public TenantInfo? CurrentTenant => _currentTenant;
    
    /// <inheritdoc />
    public bool IsValid => _currentTenant?.IsValid == true;
    
    /// <inheritdoc />
    public bool IsSystemTenant => _currentTenant?.IsSystemTenant == true;
    
    /// <inheritdoc />
    public void SetTenant(TenantInfo tenantInfo)
    {
        ArgumentNullException.ThrowIfNull(tenantInfo);
        
        if (!tenantInfo.IsValid)
        {
            _logger.LogWarning("Attempting to set invalid tenant: {TenantId}, {Subdomain}", 
                tenantInfo.Id, tenantInfo.Subdomain);
            throw new InvalidOperationException("Cannot set invalid tenant context");
        }
        
        _currentTenant = tenantInfo;
        
        _logger.LogDebug("Tenant context set: {TenantId} ({Subdomain})", 
            tenantInfo.Id, tenantInfo.Subdomain);
    }
    
    /// <inheritdoc />
    public void SetTenant(Guid tenantId, string subdomain, string name)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        
        if (string.IsNullOrWhiteSpace(subdomain))
            throw new ArgumentException("Subdomain cannot be empty", nameof(subdomain));
        
        var tenantInfo = new TenantInfo
        {
            Id = tenantId,
            Subdomain = subdomain,
            Name = name,
            Status = Core.Enums.TenantStatus.Active, // Assume active when setting manually
            CreatedAt = DateTime.UtcNow
        };
        
        SetTenant(tenantInfo);
    }
    
    /// <inheritdoc />
    public void Clear()
    {
        if (_currentTenant != null)
        {
            _logger.LogDebug("Clearing tenant context for: {TenantId} ({Subdomain})", 
                _currentTenant.Id, _currentTenant.Subdomain);
        }
        
        _currentTenant = null;
    }
    
    /// <inheritdoc />
    public Task<TenantConfiguration?> GetTenantConfigurationAsync()
    {
        if (_currentTenant == null)
        {
            _logger.LogWarning("Attempted to get tenant configuration without a tenant context");
            return Task.FromResult<TenantConfiguration?>(null);
        }
        
        return Task.FromResult<TenantConfiguration?>(_currentTenant.Configuration);
    }
    
    /// <inheritdoc />
    public Task<bool> ValidateTenantAccessAsync()
    {
        if (_currentTenant == null)
        {
            _logger.LogWarning("Attempted to validate tenant access without a tenant context");
            return Task.FromResult(false);
        }
        
        var canAccess = _currentTenant.CanAccess;
        
        if (!canAccess)
        {
            _logger.LogWarning("Tenant access denied for {TenantId} ({Subdomain}). Status: {Status}", 
                _currentTenant.Id, _currentTenant.Subdomain, _currentTenant.Status);
        }
        
        return Task.FromResult(canAccess);
    }
    
    /// <summary>
    /// Gets a string representation of the current tenant context
    /// </summary>
    public override string ToString()
    {
        return _currentTenant != null 
            ? $"Tenant: {_currentTenant.Name} ({_currentTenant.Subdomain})"
            : "No tenant context";
    }
}