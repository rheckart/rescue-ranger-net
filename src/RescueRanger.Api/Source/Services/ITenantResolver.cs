using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Services;

/// <summary>
/// Interface for tenant resolution strategies
/// </summary>
public interface ITenantResolver
{
    /// <summary>
    /// Resolves tenant information from the current context
    /// </summary>
    /// <param name="context">The resolution context (e.g., HTTP context)</param>
    /// <returns>Tenant information if found, null otherwise</returns>
    Task<TenantInfo?> ResolveTenantAsync(object context);
    
    /// <summary>
    /// Gets the tenant identifier from the context
    /// </summary>
    /// <param name="context">The resolution context</param>
    /// <returns>Tenant identifier (e.g., subdomain) if found, null otherwise</returns>
    string? GetTenantIdentifier(object context);
    
    /// <summary>
    /// Validates if the resolved tenant can be accessed
    /// </summary>
    /// <param name="tenantInfo">The tenant information</param>
    /// <returns>True if tenant can be accessed, false otherwise</returns>
    bool CanAccessTenant(TenantInfo? tenantInfo);
}

/// <summary>
/// Specialized interface for subdomain-based tenant resolution
/// </summary>
public interface ISubdomainTenantResolver : ITenantResolver
{
    /// <summary>
    /// Extracts subdomain from the host
    /// </summary>
    /// <param name="host">The host string</param>
    /// <returns>Subdomain if found, null otherwise</returns>
    string? ExtractSubdomain(string host);
    
    /// <summary>
    /// Validates if the subdomain is valid
    /// </summary>
    /// <param name="subdomain">The subdomain to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidSubdomain(string? subdomain);
}