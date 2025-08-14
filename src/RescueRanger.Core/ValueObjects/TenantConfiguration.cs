namespace RescueRanger.Core.ValueObjects;

/// <summary>
/// Configuration settings specific to a tenant
/// </summary>
public class TenantConfiguration
{
    /// <summary>
    /// Maximum number of users allowed for this tenant
    /// </summary>
    public int MaxUsers { get; set; } = 10;
    
    /// <summary>
    /// Maximum number of horses that can be managed
    /// </summary>
    public int MaxHorses { get; set; } = 100;
    
    /// <summary>
    /// Whether the tenant has access to advanced features
    /// </summary>
    public bool AdvancedFeaturesEnabled { get; set; } = false;
    
    /// <summary>
    /// Storage limit in MB for the tenant
    /// </summary>
    public int StorageLimitMb { get; set; } = 1024;
    
    /// <summary>
    /// Custom branding settings
    /// </summary>
    public BrandingSettings Branding { get; set; } = new();
    
    /// <summary>
    /// Feature flags specific to this tenant
    /// </summary>
    public Dictionary<string, bool> FeatureFlags { get; set; } = new();
    
    /// <summary>
    /// Custom metadata for the tenant
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Branding configuration for a tenant
/// </summary>
public class BrandingSettings
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