using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Endpoint for retrieving tenants with pagination and filtering
/// </summary>
sealed class GetTenantsEndpoint : Endpoint<GetTenantsRequest, TenantListResponse>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<GetTenantsEndpoint> _logger;

    public GetTenantsEndpoint(ITenantService tenantService, ILogger<GetTenantsEndpoint> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("admin/tenants");
        Summary(s =>
        {
            s.Summary = "Get tenants with pagination";
            s.Description = "Retrieves a paginated list of tenants with optional filtering by status and search term";
            s.Responses[200] = "Tenants retrieved successfully";
            s.Responses[400] = "Invalid request parameters";
            s.Responses[500] = "Internal server error";
        });
        
        // Only system administrators can view all tenants
        Policies(TenantAuthorizationPolicies.SystemAdmin);
    }

    public override async Task HandleAsync(GetTenantsRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Received request to get tenants - Page: {Page}, PageSize: {PageSize}, Status: {Status}, SearchTerm: {SearchTerm}", 
            req.Page, req.PageSize, req.Status, req.SearchTerm);

        // Validate request parameters
        if (req.Page < 1)
        {
            AddError("Page number must be greater than 0");
            await SendAsync(Results.BadRequest());
            return;
        }

        if (req.PageSize < 1 || req.PageSize > 100)
        {
            AddError("Page size must be between 1 and 100");
            await SendAsync(Results.BadRequest());
            return;
        }

        var result = await _tenantService.GetTenantsAsync(req, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully retrieved {Count} tenants (Page {Page} of {TotalPages})", 
                result.Value.Tenants.Count, result.Value.Pagination.CurrentPage, result.Value.Pagination.TotalPages);
            
            Response = result.Value;
        }
        else
        {
            _logger.LogError("Error retrieving tenants: {Error}", result.Errors.FirstOrDefault());
            
            AddError("An error occurred while retrieving tenants");
            await SendAsync(Results.Problem("An error occurred while retrieving tenants"));
        }
    }
}