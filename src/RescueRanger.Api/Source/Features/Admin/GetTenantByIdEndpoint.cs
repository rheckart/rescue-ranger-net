using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Request for getting a tenant by ID
/// </summary>
sealed class GetTenantByIdRequest
{
    /// <summary>
    /// The tenant ID to retrieve
    /// </summary>
    public Guid TenantId { get; set; }
}

/// <summary>
/// Endpoint for retrieving detailed information about a specific tenant
/// </summary>
sealed class GetTenantByIdEndpoint : Endpoint<GetTenantByIdRequest, TenantResponse>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<GetTenantByIdEndpoint> _logger;

    public GetTenantByIdEndpoint(ITenantService tenantService, ILogger<GetTenantByIdEndpoint> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("admin/tenants/{tenantId}");
        Summary(s =>
        {
            s.Summary = "Get tenant by ID";
            s.Description = "Retrieves detailed information about a specific tenant by their unique identifier";
            s.Responses[200] = "Tenant details retrieved successfully";
            s.Responses[404] = "Tenant not found";
            s.Responses[400] = "Invalid tenant ID format";
            s.Responses[500] = "Internal server error";
        });
        
        // Only system administrators can view tenant details
        Policies(TenantAuthorizationPolicies.SystemAdmin);
    }

    public override async Task HandleAsync(GetTenantByIdRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Received request to get tenant by ID: {TenantId}", req.TenantId);

        if (req.TenantId == Guid.Empty)
        {
            _logger.LogWarning("Invalid tenant ID provided: {TenantId}", req.TenantId);
            AddError("Invalid tenant ID format");
            await Send.ResultAsync(Results.BadRequest("Invalid tenant ID format"));
            return;
        }

        var result = await _tenantService.GetTenantByIdAsync(req.TenantId);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully retrieved tenant {TenantId}: {TenantName}", 
                result.Value.Id, result.Value.Name);
            
            await Send.OkAsync(result.Value);
        }
        else if (result.Status == Ardalis.Result.ResultStatus.NotFound)
        {
            _logger.LogWarning("Tenant not found: {TenantId}", req.TenantId);
            
            AddError($"Tenant with ID {req.TenantId} was not found");
            await Send.ResultAsync(Results.NotFound($"Tenant with ID {req.TenantId} was not found"));
        }
        else
        {
            _logger.LogError("Error retrieving tenant {TenantId}: {Error}", req.TenantId, result.Errors.FirstOrDefault());
            
            AddError("An error occurred while retrieving the tenant");
            await Send.ResultAsync(Results.Problem("An error occurred while retrieving the tenant"));
        }
    }
}