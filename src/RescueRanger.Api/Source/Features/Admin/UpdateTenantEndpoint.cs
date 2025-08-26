using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Combined request for updating tenant with route parameter
/// </summary>
sealed class UpdateTenantRequestWithId : UpdateTenantRequest
{
    /// <summary>
    /// The tenant ID to update
    /// </summary>
    public Guid TenantId { get; set; }
}

/// <summary>
/// Endpoint for updating tenant configuration and settings
/// </summary>
sealed class UpdateTenantEndpoint : Endpoint<UpdateTenantRequestWithId, TenantResponse>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<UpdateTenantEndpoint> _logger;

    public UpdateTenantEndpoint(ITenantService tenantService, ILogger<UpdateTenantEndpoint> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    public override void Configure()
    {
        Put("admin/tenants/{tenantId}");
        Summary(s =>
        {
            s.Summary = "Update tenant configuration";
            s.Description = "Updates tenant information and configuration settings";
            s.Responses[200] = "Tenant updated successfully";
            s.Responses[400] = "Validation failed";
            s.Responses[404] = "Tenant not found";
            s.Responses[500] = "Internal server error";
        });
        
        // Only system administrators can update tenants
        Policies(TenantAuthorizationPolicies.SystemAdmin);
        // Policies("AdminOnly");
    }

    public override async Task HandleAsync(UpdateTenantRequestWithId req, CancellationToken ct)
    {
        _logger.LogInformation("Received request to update tenant: {TenantId}", req.TenantId);

        if (req.TenantId == Guid.Empty)
        {
            _logger.LogWarning("Invalid tenant ID provided: {TenantId}", req.TenantId);
            AddError("Invalid tenant ID format");
            await SendAsync(Results.BadRequest(ValidationFailures), ct);
            return;
        }

        var updateRequest = new UpdateTenantRequest
        {
            Name = req.Name,
            ContactEmail = req.ContactEmail,
            PhoneNumber = req.PhoneNumber,
            Address = req.Address,
            Configuration = req.Configuration
        };

        var result = await _tenantService.UpdateTenantAsync(req.TenantId, updateRequest, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully updated tenant {TenantId}: {TenantName}", 
                result.Value.Id, result.Value.Name);
            
            await Send.OkAsync(result.Value, ct);
        }
        else if (result.Status == Ardalis.Result.ResultStatus.NotFound)
        {
            _logger.LogWarning("Tenant not found for update: {TenantId}", req.TenantId);
            
            AddError($"Tenant with ID {req.TenantId} was not found");
            await SendAsync(Results.NotFound(ValidationFailures), ct);
        }
        else if (result.Status == Ardalis.Result.ResultStatus.Invalid)
        {
            _logger.LogWarning("Validation failed for tenant update {TenantId}: {Errors}", 
                req.TenantId, string.Join(", ", result.ValidationErrors.Select(e => e.ErrorMessage)));
            
            foreach (var error in result.ValidationErrors)
            {
                AddError(error.ErrorMessage);
            }
            await SendAsync(Results.BadRequest(ValidationFailures), ct);
        }
        else
        {
            _logger.LogError("Error updating tenant {TenantId}: {Error}", req.TenantId, result.Errors.FirstOrDefault());
            
            AddError("An error occurred while updating the tenant");
            await SendAsync(Results.Problem("An error occurred while updating the tenant"), ct);
        }
    }
}