using FluentValidation;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Request model for updating tenant configuration
/// </summary>
public class UpdateTenantRequest
{
    /// <summary>
    /// Name of the organization
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Primary contact email for the organization
    /// </summary>
    public string? ContactEmail { get; set; }
    
    /// <summary>
    /// Phone number for the organization
    /// </summary>
    public string? PhoneNumber { get; set; }
    
    /// <summary>
    /// Physical address of the organization
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Configuration settings to update
    /// </summary>
    public UpdateTenantConfigurationRequest? Configuration { get; set; }
}

/// <summary>
/// Configuration settings for tenant update
/// </summary>
public class UpdateTenantConfigurationRequest
{
    /// <summary>
    /// Maximum number of horses allowed
    /// </summary>
    public int? MaxHorses { get; set; }
    
    /// <summary>
    /// Maximum number of volunteers allowed
    /// </summary>
    public int? MaxVolunteers { get; set; }
    
    /// <summary>
    /// Whether email notifications are enabled
    /// </summary>
    public bool? EmailNotificationsEnabled { get; set; }
    
    /// <summary>
    /// Whether SMS notifications are enabled
    /// </summary>
    public bool? SmsNotificationsEnabled { get; set; }
    
    /// <summary>
    /// Custom branding colors and settings
    /// </summary>
    public Dictionary<string, object>? CustomSettings { get; set; }
}

/// <summary>
/// Validator for UpdateTenantRequest
/// </summary>
public class UpdateTenantRequestValidator : Validator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .WithMessage("Organization name must be 200 characters or less")
            .When(x => !string.IsNullOrEmpty(x.Name));
        
        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .WithMessage("Contact email must be a valid email address")
            .MaximumLength(320)
            .WithMessage("Contact email must be 320 characters or less")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));
        
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Phone number must be in E.164 format (e.g., +1234567890)")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        
        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Address must be 500 characters or less")
            .When(x => !string.IsNullOrEmpty(x.Address));
        
        RuleFor(x => x.Configuration!.MaxHorses!.Value)
            .GreaterThan(0)
            .WithMessage("MaxHorses must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("MaxHorses cannot exceed 10,000")
            .When(x => x.Configuration?.MaxHorses.HasValue == true);
        
        RuleFor(x => x.Configuration!.MaxVolunteers!.Value)
            .GreaterThan(0)
            .WithMessage("MaxVolunteers must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("MaxVolunteers cannot exceed 1,000")
            .When(x => x.Configuration?.MaxVolunteers.HasValue == true);
    }
}