namespace RescueRanger.Core.Models;

/// <summary>
/// Configuration options for multi-tenant functionality
/// </summary>
public class MultiTenantOptions
{
    /// <summary>
    /// The base domain for the application (e.g., "rescueranger.com")
    /// </summary>
    public string BaseDomain { get; set; } = "rescueranger.com";
    
    /// <summary>
    /// Cache expiration time in minutes
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// Tenant for development environment
    /// </summary>
    public string? DevelopmentTenant { get; set; }
    
    /// <summary>
    /// Whether to require HTTPS for tenant resolution
    /// </summary>
    public bool RequireHttps { get; set; } = true;
    
    /// <summary>
    /// List of reserved subdomains that cannot be used by tenants
    /// </summary>
    public List<string> ReservedSubdomains { get; set; } = new()
    {
        "www", "api", "admin", "app", "mail", "ftp", "ssl", "cdn", "blog", "help", "support", "assets"
    };
    
    /// <summary>
    /// Whether to enable tenant resolution in development mode
    /// </summary>
    public bool EnableInDevelopment { get; set; } = true;
    
    /// <summary>
    /// Default tenant configuration for new tenants
    /// </summary>
    public DefaultTenantConfiguration DefaultConfiguration { get; set; } = new();
}

/// <summary>
/// Default configuration applied to new tenants
/// </summary>
public class DefaultTenantConfiguration
{
    /// <summary>
    /// Default maximum number of users
    /// </summary>
    public int MaxUsers { get; set; } = 10;
    
    /// <summary>
    /// Default maximum number of horses
    /// </summary>
    public int MaxHorses { get; set; } = 100;
    
    /// <summary>
    /// Default storage limit in MB
    /// </summary>
    public int StorageLimitMb { get; set; } = 1024;
    
    /// <summary>
    /// Whether advanced features are enabled by default
    /// </summary>
    public bool AdvancedFeaturesEnabled { get; set; } = false;
}