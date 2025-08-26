namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Response model for paginated tenant listing
/// </summary>
public class TenantListResponse
{
    /// <summary>
    /// List of tenants for the current page
    /// </summary>
    public List<TenantSummaryResponse> Tenants { get; set; } = [];
    
    /// <summary>
    /// Pagination metadata
    /// </summary>
    public PaginationMetadata Pagination { get; set; } = new();
}

/// <summary>
/// Summary response model for tenant in list view
/// </summary>
public class TenantSummaryResponse
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
    /// Indicates if this is the system admin tenant
    /// </summary>
    public bool IsSystemTenant { get; set; }
    
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
/// Pagination metadata for list responses
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }
    
    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public long TotalItems { get; set; }
    
    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
    
    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }
    
    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }
}

/// <summary>
/// Request model for getting tenants with pagination and filtering
/// </summary>
public class GetTenantsRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Number of items per page (max 100)
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// Filter by tenant status
    /// </summary>
    public string? Status { get; set; }
    
    /// <summary>
    /// Search term for tenant name or subdomain
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// Sort field (Name, Subdomain, CreatedAt, Status)
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";
    
    /// <summary>
    /// Sort direction (asc or desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}