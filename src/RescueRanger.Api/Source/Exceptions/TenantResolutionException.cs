namespace RescueRanger.Api.Exceptions;

/// <summary>
/// Base exception for tenant resolution failures
/// </summary>
public class TenantResolutionException : Exception
{
    public string? TenantIdentifier { get; }
    
    public TenantResolutionException(string message, string? tenantIdentifier = null) 
        : base(message)
    {
        TenantIdentifier = tenantIdentifier;
    }
    
    public TenantResolutionException(string message, string? tenantIdentifier, Exception innerException) 
        : base(message, innerException)
    {
        TenantIdentifier = tenantIdentifier;
    }
}

/// <summary>
/// Exception thrown when a tenant cannot be found
/// </summary>
public class TenantNotFoundException : TenantResolutionException
{
    public TenantNotFoundException(string tenantIdentifier) 
        : base($"Tenant not found: {tenantIdentifier}", tenantIdentifier)
    {
    }
}

/// <summary>
/// Exception thrown when a tenant is suspended or inactive
/// </summary>
public class TenantInactiveException : TenantResolutionException
{
    public Guid TenantId { get; }
    public string Status { get; }
    
    public TenantInactiveException(Guid tenantId, string tenantIdentifier, string status) 
        : base($"Tenant '{tenantIdentifier}' is {status}", tenantIdentifier)
    {
        TenantId = tenantId;
        Status = status;
    }
}

/// <summary>
/// Exception thrown when tenant validation fails
/// </summary>
public class TenantValidationException : TenantResolutionException
{
    public IEnumerable<string> ValidationErrors { get; }
    
    public TenantValidationException(string tenantIdentifier, IEnumerable<string> errors) 
        : base($"Tenant validation failed for '{tenantIdentifier}': {string.Join(", ", errors)}", tenantIdentifier)
    {
        ValidationErrors = errors;
    }
}

/// <summary>
/// Exception thrown when tenant access is denied
/// </summary>
public class TenantAccessDeniedException : TenantResolutionException
{
    public Guid? UserId { get; }
    public Guid TenantId { get; }
    
    public TenantAccessDeniedException(Guid tenantId, Guid? userId = null, string? reason = null) 
        : base($"Access denied to tenant. {reason ?? "Insufficient permissions."}")
    {
        TenantId = tenantId;
        UserId = userId;
    }
}

/// <summary>
/// Exception thrown when subdomain format is invalid
/// </summary>
public class InvalidSubdomainException : TenantResolutionException
{
    public InvalidSubdomainException(string subdomain) 
        : base($"Invalid subdomain format: '{subdomain}'. Subdomains must be 3-63 characters long and contain only lowercase letters, numbers, and hyphens.", subdomain)
    {
    }
}

/// <summary>
/// Exception thrown when subdomain is reserved
/// </summary>
public class ReservedSubdomainException : TenantResolutionException
{
    public ReservedSubdomainException(string subdomain) 
        : base($"The subdomain '{subdomain}' is reserved and cannot be used.", subdomain)
    {
    }
}