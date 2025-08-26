using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Endpoint for creating new tenants
/// </summary>
sealed class CreateTenantEndpoint : Endpoint<CreateTenantRequest, TenantResponse>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<CreateTenantEndpoint> _logger;

    public CreateTenantEndpoint(ITenantService tenantService, ILogger<CreateTenantEndpoint> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("admin/tenants");
        Summary(s =>
        {
            s.Summary = "Create a new tenant";
            s.Description = "Creates a new tenant organization with subdomain validation and automatic provisioning";
            s.Responses[201] = "Tenant created successfully";
            s.Responses[400] = "Validation failed";
            s.Responses[409] = "Subdomain already exists";
            s.Responses[500] = "Internal server error";
        });
        
        // Only system administrators can create tenants
        Policies(TenantAuthorizationPolicies.SystemAdmin);
    }

    public override async Task HandleAsync(CreateTenantRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Received request to create tenant with subdomain: {Subdomain}", req.Subdomain);

        var result = await _tenantService.CreateTenantAsync(req, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully created tenant {TenantId} with subdomain {Subdomain}", 
                result.Value.Id, result.Value.Subdomain);
            
            Response = result.Value;
            HttpContext.Response.StatusCode = 201;
        }
        else
        {
            _logger.LogError("Error creating tenant with subdomain {Subdomain}: {Errors}", 
                req.Subdomain, string.Join(", ", result.Errors));
            
            AddError("Failed to create tenant: " + string.Join(", ", result.Errors));
            await Send.ResultAsync(Results.Problem("Failed to create tenant"));
        }
    }
}