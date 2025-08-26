using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.Admin;

/// <summary>
/// Combined request for suspending tenant with route parameter
/// </summary>
sealed class SuspendTenantRequestWithId : SuspendTenantRequest
{
    /// <summary>
    /// The tenant ID to suspend
    /// </summary>
    public Guid TenantId { get; set; }
}

/// <summary>
/// Endpoint for suspending a tenant
/// </summary>
sealed class SuspendTenantEndpoint : Endpoint<SuspendTenantRequestWithId, TenantResponse>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<SuspendTenantEndpoint> _logger;

    public SuspendTenantEndpoint(ITenantService tenantService, ILogger<SuspendTenantEndpoint> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("admin/tenants/{tenantId}/suspend");
        Summary(s =>
        {
            s.Summary = "Suspend a tenant";
            s.Description = "Suspends a tenant with a reason, optionally scheduling the suspension for later";
            s.Responses[200] = "Tenant suspended successfully";
            s.Responses[400] = "Validation failed";
            s.Responses[404] = "Tenant not found";
            s.Responses[409] = "Tenant already suspended or cannot be suspended";
            s.Responses[403] = "System tenant cannot be suspended";
            s.Responses[500] = "Internal server error";
        });
        
        // Only system administrators can suspend tenants
        Policies(TenantAuthorizationPolicies.SystemAdmin);
        // Policies("AdminOnly");
    }

    public override async Task HandleAsync(SuspendTenantRequestWithId req, CancellationToken ct)
    {
        _logger.LogInformation("Received request to suspend tenant: {TenantId}, Reason: {Reason}", 
            req.TenantId, req.Reason);

        if (req.TenantId == Guid.Empty)
        {
            _logger.LogWarning("Invalid tenant ID provided: {TenantId}", req.TenantId);
            AddError("Invalid tenant ID format");
            await Send.ResultAsync(Results.BadRequest("Invalid tenant ID format"));
            return;
        }

        var suspendRequest = new SuspendTenantRequest
        {
            Reason = req.Reason,
            ImmediateSuspension = req.ImmediateSuspension,
            ScheduledSuspensionAt = req.ScheduledSuspensionAt,
            NotifyTenant = req.NotifyTenant
        };

        var result = await _tenantService.SuspendTenantAsync(req.TenantId, suspendRequest);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully suspended tenant {TenantId}: {TenantName}", 
                result.Value.Id, result.Value.Name);
            
            await Send.OkAsync(result.Value);
        }
        else if (result.Status == Ardalis.Result.ResultStatus.NotFound)
        {
            _logger.LogWarning("Tenant not found for suspension: {TenantId}", req.TenantId);
            
            AddError($"Tenant with ID {req.TenantId} was not found");
            await Send.ResultAsync(Results.NotFound($"Tenant with ID {req.TenantId} was not found"));
        }
        else if (result.Status == Ardalis.Result.ResultStatus.Invalid)
        {
            _logger.LogWarning("Invalid suspension request for tenant {TenantId}: {Errors}", 
                req.TenantId, string.Join(", ", result.ValidationErrors.Select(e => e.ErrorMessage)));
            
            foreach (var error in result.ValidationErrors)
            {
                AddError(error.ErrorMessage);
            }
            await Send.ResultAsync(Results.BadRequest(string.Join(", ", result.ValidationErrors.Select(e => e.ErrorMessage))));
        }
        else if (result.Status == Ardalis.Result.ResultStatus.Forbidden)
        {
            _logger.LogWarning("Forbidden suspension attempt for tenant {TenantId}: {Error}", 
                req.TenantId, result.Errors.FirstOrDefault());
            
            AddError(result.Errors.FirstOrDefault() ?? "This tenant cannot be suspended");
            await Send.ResultAsync(Results.Forbid());
        }
        else if (result.Status == Ardalis.Result.ResultStatus.Conflict)
        {
            _logger.LogWarning("Conflict suspending tenant {TenantId}: {Error}", 
                req.TenantId, result.Errors.FirstOrDefault());
            
            AddError(result.Errors.FirstOrDefault() ?? "Tenant is already suspended");
            await Send.ResultAsync(Results.Conflict(result.Errors.FirstOrDefault() ?? "Tenant is already suspended"));
        }
        else
        {
            _logger.LogError("Error suspending tenant {TenantId}: {Error}", req.TenantId, result.Errors.FirstOrDefault());
            
            AddError("An error occurred while suspending the tenant");
            await Send.ResultAsync(Results.Problem("An error occurred while suspending the tenant"));
        }
    }
}

/// <summary>
/// Endpoint for reactivating a suspended tenant
/// </summary>
sealed class ReactivateTenantEndpoint : EndpointWithoutRequest<TenantResponse>
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<ReactivateTenantEndpoint> _logger;

    public ReactivateTenantEndpoint(ITenantService tenantService, ILogger<ReactivateTenantEndpoint> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("admin/tenants/{tenantId}/reactivate");
        Summary(s =>
        {
            s.Summary = "Reactivate a suspended tenant";
            s.Description = "Reactivates a suspended tenant, allowing them to access the system again";
            s.Responses[200] = "Tenant reactivated successfully";
            s.Responses[404] = "Tenant not found";
            s.Responses[409] = "Tenant is not suspended";
            s.Responses[500] = "Internal server error";
        });
        
        // Only system administrators can suspend tenants
        Policies(TenantAuthorizationPolicies.SystemAdmin);
        // Policies("AdminOnly");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tenantId = Route<Guid>("tenantId");
        _logger.LogInformation("Received request to reactivate tenant: {TenantId}", tenantId);

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("Invalid tenant ID provided: {TenantId}", tenantId);
            AddError("Invalid tenant ID format");
            await Send.ResultAsync(Results.BadRequest("Invalid tenant ID format"));
            return;
        }

        var result = await _tenantService.ReactivateTenantAsync(tenantId);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully reactivated tenant {TenantId}: {TenantName}", 
                result.Value.Id, result.Value.Name);
            
            await Send.OkAsync(result.Value);
        }
        else if (result.Status == Ardalis.Result.ResultStatus.NotFound)
        {
            _logger.LogWarning("Tenant not found for reactivation: {TenantId}", tenantId);
            
            AddError($"Tenant with ID {tenantId} was not found");
            await Send.ResultAsync(Results.NotFound($"Tenant with ID {tenantId} was not found"));
        }
        else if (result.Status == Ardalis.Result.ResultStatus.Conflict)
        {
            _logger.LogWarning("Invalid reactivation request for tenant {TenantId}: {Error}", 
                tenantId, result.Errors.FirstOrDefault());
            
            AddError(result.Errors.FirstOrDefault() ?? "This tenant cannot be reactivated");
            await Send.ResultAsync(Results.Conflict(result.Errors.FirstOrDefault() ?? "This tenant cannot be reactivated"));
        }
        else
        {
            _logger.LogError("Error reactivating tenant {TenantId}: {Error}", tenantId, result.Errors.FirstOrDefault());
            
            AddError("An error occurred while reactivating the tenant");
            await Send.ResultAsync(Results.Problem("An error occurred while reactivating the tenant"));
        }
    }
}