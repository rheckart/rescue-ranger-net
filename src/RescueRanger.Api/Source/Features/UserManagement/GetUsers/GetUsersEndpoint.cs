using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;
using RescueRanger.Infrastructure.Data;

namespace RescueRanger.Api.Features.UserManagement.GetUsers;

/// <summary>
/// Response for getting tenant users
/// </summary>
public class GetUsersResponse
{
    /// <summary>
    /// List of users in the tenant
    /// </summary>
    public List<TenantUserDto> Users { get; set; } = [];
    
    /// <summary>
    /// Total count of users
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
}

/// <summary>
/// User DTO for tenant user list
/// </summary>
public class TenantUserDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Role in tenant
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether user is active
    /// </summary>
    public bool IsActive { get; set; }
    
    /// <summary>
    /// Whether email is confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; }
    
    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// When user was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request for getting tenant users
/// </summary>
public class GetUsersRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// Filter by role
    /// </summary>
    public string? Role { get; set; }
    
    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Search term for name or email
    /// </summary>
    public string? Search { get; set; }
}

/// <summary>
/// Endpoint for getting users in the current tenant
/// </summary>
public class GetUsersEndpoint : Endpoint<GetUsersRequest, GetUsersResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContextService _tenantContext;
    private readonly ILogger<GetUsersEndpoint> _logger;
    
    public GetUsersEndpoint(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        ILogger<GetUsersEndpoint> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }
    
    public override void Configure()
    {
        Get("/users");
        Policies(TenantAuthorizationPolicies.TenantUser);
        Summary(s => s
            .Summary("Get users in the current tenant")
            .Description("Returns a paginated list of users in the current tenant")
            .Response(200, "Users retrieved successfully")
            .Response(403, "Insufficient permissions"));
    }
    
    public override async Task HandleAsync(GetUsersRequest req, CancellationToken ct)
    {
        try
        {
            if (!_tenantContext.IsValid)
            {
                await Send.ForbidAsync(ct);
                return;
            }
            
            var query = _context.Users.AsQueryable();
            
            // Apply filters
            if (!string.IsNullOrEmpty(req.Role))
            {
                query = query.Where(u => u.Role.ToLower() == req.Role.ToLower());
            }
            
            if (req.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == req.IsActive.Value);
            }
            
            if (!string.IsNullOrEmpty(req.Search))
            {
                var searchTerm = req.Search.ToLower();
                query = query.Where(u => 
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm));
            }
            
            // Get total count
            var totalCount = await query.CountAsync(ct);
            
            // Apply pagination
            var users = await query
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .Select(u => new TenantUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    EmailConfirmed = u.EmailConfirmed,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync(ct);
                
            var response = new GetUsersResponse
            {
                Users = users,
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize
            };
            
            await Send.OkAsync(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for tenant {TenantId}", _tenantContext.TenantId);
            await SendAsync(Results.Problem("An error occurred while retrieving users"));
        }
    }
}