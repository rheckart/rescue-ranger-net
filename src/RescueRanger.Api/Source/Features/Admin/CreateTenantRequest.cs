using FluentValidation;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Request model for creating a new tenant
/// </summary>
public class CreateTenantRequest
{
    /// <summary>
    /// Name of the organization
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Unique subdomain for the tenant (e.g., "rescue1" in rescue1.rescueranger.com)
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
    /// Initial configuration settings for this tenant
    /// </summary>
    public TenantConfigurationRequest? Configuration { get; set; }
}

/// <summary>
/// Configuration settings for tenant creation
/// </summary>
public class TenantConfigurationRequest
{
    /// <summary>
    /// Maximum number of horses allowed
    /// </summary>
    public int MaxHorses { get; set; } = 100;
    
    /// <summary>
    /// Maximum number of volunteers allowed
    /// </summary>
    public int MaxVolunteers { get; set; } = 50;
    
    /// <summary>
    /// Whether email notifications are enabled
    /// </summary>
    public bool EmailNotificationsEnabled { get; set; } = true;
    
    /// <summary>
    /// Whether SMS notifications are enabled
    /// </summary>
    public bool SmsNotificationsEnabled { get; set; } = false;
    
    /// <summary>
    /// Custom branding colors and settings
    /// </summary>
    public Dictionary<string, object>? CustomSettings { get; set; }
}

/// <summary>
/// Validator for CreateTenantRequest
/// </summary>
public class CreateTenantRequestValidator : Validator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Organization name is required")
            .MaximumLength(200)
            .WithMessage("Organization name must be 200 characters or less");
        
        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .WithMessage("Subdomain is required")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Subdomain can only contain lowercase letters, numbers, and hyphens")
            .Length(3, 50)
            .WithMessage("Subdomain must be between 3 and 50 characters")
            .Must(x => !x.StartsWith("-") && !x.EndsWith("-"))
            .WithMessage("Subdomain cannot start or end with a hyphen");
        
        RuleFor(x => x.ContactEmail)
            .NotEmpty()
            .WithMessage("Contact email is required")
            .EmailAddress()
            .WithMessage("Contact email must be a valid email address")
            .MaximumLength(320)
            .WithMessage("Contact email must be 320 characters or less");
        
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Phone number must be in E.164 format (e.g., +1234567890)")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        
        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address must be 500 characters or less")
            .When(x => !string.IsNullOrEmpty(x.Address));
        
        RuleFor(x => x.Configuration!.MaxHorses)
            .GreaterThan(0)
            .WithMessage("MaxHorses must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("MaxHorses cannot exceed 10,000")
            .When(x => x.Configuration is not null);
        
        RuleFor(x => x.Configuration!.MaxVolunteers)
            .GreaterThan(0)
            .WithMessage("MaxVolunteers must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("MaxVolunteers cannot exceed 1,000")
            .When(x => x.Configuration is not null);
    }
}