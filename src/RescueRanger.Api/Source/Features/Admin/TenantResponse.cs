using RescueRanger.Api.Entities;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Response model for tenant information
/// </summary>
public class TenantResponse
{
    /// <summary>
    /// Unique identifier for the tenant
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Name of the organization
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique subdomain for the tenant
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;
    
    /// <summary>
    /// Primary contact email for the organization
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Phone number for the organization
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Physical address of the organization
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Current status of the tenant
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// When the tenant was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the tenant was activated
    /// </summary>
    public DateTime? ActivatedAt { get; set; }
    
    /// <summary>
    /// When the tenant was suspended (if applicable)
    /// </summary>
    public DateTime? SuspendedAt { get; set; }
    
    /// <summary>
    /// Reason for suspension (if applicable)
    /// </summary>
    public string? SuspensionReason { get; set; }
    
    /// <summary>
    /// Configuration settings for this tenant
    /// </summary>
    public TenantConfigurationResponse Configuration { get; set; } = new();
    
    /// <summary>
    /// Indicates if this is the system admin tenant
    /// </summary>
    public bool IsSystemTenant { get; set; }
    
    /// <summary>
    /// When the API key was last rotated
    /// </summary>
    public DateTime? ApiKeyRotatedAt { get; set; }
    
    /// <summary>
    /// Indicates if the tenant is currently active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Indicates if the tenant can be accessed
    /// </summary>
    public bool CanAccess { get; set; }
}

/// <summary>
/// Configuration settings response for tenant
/// </summary>
public class TenantConfigurationResponse
{
    /// <summary>
    /// Maximum number of horses allowed
    /// </summary>
    public int MaxHorses { get; set; }
    
    /// <summary>
    /// Maximum number of users allowed
    /// </summary>
    public int MaxUsers { get; set; }
    
    /// <summary>
    /// Whether advanced features are enabled
    /// </summary>
    public bool AdvancedFeaturesEnabled { get; set; }
    
    /// <summary>
    /// Storage limit in MB
    /// </summary>
    public int StorageLimitMb { get; set; }
    
    /// <summary>
    /// Feature flags for the tenant
    /// </summary>
    public Dictionary<string, bool> FeatureFlags { get; set; } = new();
    
    /// <summary>
    /// Custom metadata for the tenant
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Branding settings
    /// </summary>
    public BrandingSettingsResponse? Branding { get; set; }
}

/// <summary>
/// Branding settings response
/// </summary>
public class BrandingSettingsResponse
{
    /// <summary>
    /// Primary color for the tenant's UI
    /// </summary>
    public string PrimaryColor { get; set; } = "#1976D2";
    
    /// <summary>
    /// Secondary color for the tenant's UI
    /// </summary>
    public string SecondaryColor { get; set; } = "#424242";
    
    /// <summary>
    /// URL to the tenant's logo
    /// </summary>
    public string? LogoUrl { get; set; }
    
    /// <summary>
    /// URL to the tenant's favicon
    /// </summary>
    public string? FaviconUrl { get; set; }
    
    /// <summary>
    /// Custom CSS overrides
    /// </summary>
    public string? CustomCss { get; set; }
}

/// <summary>
/// Mapper for tenant entities to responses
/// </summary>
public static class TenantResponseMapper
{
    /// <summary>
    /// Maps a Tenant entity to a TenantResponse
    /// </summary>
    public static TenantResponse ToResponse(Tenant tenant)
    {
        return new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            ContactEmail = tenant.ContactEmail,
            PhoneNumber = tenant.PhoneNumber,
            Address = tenant.Address,
            Status = tenant.Status,
            CreatedAt = tenant.CreatedAt,
            ActivatedAt = tenant.ActivatedAt,
            SuspendedAt = tenant.SuspendedAt,
            SuspensionReason = tenant.SuspensionReason,
            IsSystemTenant = tenant.IsSystemTenant,
            ApiKeyRotatedAt = tenant.ApiKeyRotatedAt,
            IsActive = tenant.IsActive(),
            CanAccess = tenant.CanAccess(),
            Configuration = ToConfigurationResponse(tenant.Configuration)
        };
    }
    
    /// <summary>
    /// Maps a TenantConfiguration entity to a TenantConfigurationResponse
    /// </summary>
    public static TenantConfigurationResponse ToConfigurationResponse(TenantConfiguration config)
    {
        return new TenantConfigurationResponse
        {
            MaxHorses = config.MaxHorses,
            MaxUsers = config.MaxUsers,
            AdvancedFeaturesEnabled = config.AdvancedFeaturesEnabled,
            StorageLimitMb = config.StorageLimitMb,
            FeatureFlags = config.FeatureFlags,
            Metadata = config.Metadata,
            Branding = config.Branding != null ? new BrandingSettingsResponse
            {
                PrimaryColor = config.Branding.PrimaryColor,
                SecondaryColor = config.Branding.SecondaryColor,
                LogoUrl = config.Branding.LogoUrl,
                FaviconUrl = config.Branding.FaviconUrl,
                CustomCss = config.Branding.CustomCss
            } : null
        };
    }
}