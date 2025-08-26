using FluentValidation;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Request model for suspending a tenant
/// </summary>
public class SuspendTenantRequest
{
    /// <summary>
    /// Reason for suspending the tenant
    /// </summary>
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to immediately suspend or schedule for later
    /// </summary>
    public bool ImmediateSuspension { get; set; } = true;
    
    /// <summary>
    /// Optional scheduled suspension date/time (if not immediate)
    /// </summary>
    public DateTime? ScheduledSuspensionAt { get; set; }
    
    /// <summary>
    /// Whether to notify the tenant about the suspension
    /// </summary>
    public bool NotifyTenant { get; set; } = true;
}

/// <summary>
/// Validator for SuspendTenantRequest
/// </summary>
public class SuspendTenantRequestValidator : Validator<SuspendTenantRequest>
{
    public SuspendTenantRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Suspension reason is required")
            .MaximumLength(1000)
            .WithMessage("Suspension reason must be 1000 characters or less");
        
        RuleFor(x => x.ScheduledSuspensionAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Scheduled suspension date must be in the future")
            .When(x => !x.ImmediateSuspension && x.ScheduledSuspensionAt.HasValue);
        
        RuleFor(x => x.ScheduledSuspensionAt)
            .NotNull()
            .WithMessage("Scheduled suspension date is required when not suspending immediately")
            .When(x => !x.ImmediateSuspension);
    }
}