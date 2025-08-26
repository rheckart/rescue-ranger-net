using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RescueRanger.Api.Services;
using ProblemDetails = FastEndpoints.ProblemDetails;

namespace RescueRanger.Api.Authorization;

/// <summary>
/// Attribute for validating tenant context and preventing cross-tenant access
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class TenantValidationAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>
    /// Whether to require a valid tenant context (default: true)
    /// </summary>
    public bool RequireTenant { get; set; } = true;
    
    /// <summary>
    /// Whether to allow system admin bypass (default: false)
    /// </summary>
    public bool AllowSystemAdminBypass { get; set; } = false;
    
    /// <summary>
    /// Whether to validate tenant-specific resource access
    /// </summary>
    public bool ValidateResourceAccess { get; set; } = true;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContextService>();
        var userIdentity = context.HttpContext.RequestServices.GetRequiredService<ITenantUserIdentityService>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TenantValidationAttribute>>();

        try
        {
            // Skip validation for anonymous endpoints unless explicitly required
            if (!RequireTenant && !context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                await next();
                return;
            }

            // Check tenant context validity
            if (RequireTenant && !tenantContext.IsValid)
            {
                logger.LogWarning("Tenant context not established for endpoint {Endpoint}", 
                    context.ActionDescriptor.DisplayName);
                
                context.Result = new ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Tenant Not Found",
                    Detail = "Unable to resolve tenant context",
                    Status = 404,
                    Instance = context.HttpContext.Request.Path
                })
                {
                    StatusCode = 404
                };
                return;
            }

            // Allow system admin bypass if configured
            if (AllowSystemAdminBypass && userIdentity.IsSystemAdmin())
            {
                logger.LogDebug("System admin bypassing tenant validation");
                await next();
                return;
            }

            // Validate user tenant access for authenticated requests
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                if (!await userIdentity.ValidateUserTenantAccessAsync())
                {
                    logger.LogWarning("User {Email} attempted cross-tenant access to {TenantId}", 
                        userIdentity.GetCurrentUserEmail(), tenantContext.TenantId);
                    
                    context.Result = new ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Title = "Forbidden",
                        Detail = "Access denied: User does not belong to this tenant",
                        Status = 403,
                        Instance = context.HttpContext.Request.Path
                    })
                    {
                        StatusCode = 403
                    };
                    return;
                }

                // Validate resource access if requested
                if (ValidateResourceAccess)
                {
                    var resourceId = ExtractResourceId(context);
                    if (resourceId.HasValue && !await ValidateTenantResourceAccess(tenantContext, resourceId.Value))
                    {
                        logger.LogWarning("User {Email} attempted to access resource {ResourceId} in wrong tenant {TenantId}",
                            userIdentity.GetCurrentUserEmail(), resourceId, tenantContext.TenantId);
                        
                        context.Result = new ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
                        {
                            Title = "Forbidden",
                            Detail = "Access denied: Resource does not belong to this tenant",
                            Status = 403,
                            Instance = context.HttpContext.Request.Path
                        })
                        {
                            StatusCode = 403
                        };
                        return;
                    }
                }
            }

            await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during tenant validation");
            
            context.Result = new ObjectResult(new ProblemDetails
            {
                Detail = "An error occurred during tenant validation",
                Status = 500,
                Instance = context.HttpContext.Request.Path
            })
            {
                StatusCode = 500
            };
        }
    }

    private static Guid? ExtractResourceId(ActionExecutingContext context)
    {
        // Try to extract resource ID from route parameters
        if (context.RouteData.Values.TryGetValue("id", out var idValue) && 
            Guid.TryParse(idValue?.ToString(), out var id))
        {
            return id;
        }

        // Try to extract from query parameters
        if (context.HttpContext.Request.Query.TryGetValue("id", out var queryId) &&
            Guid.TryParse(queryId.FirstOrDefault(), out var queryGuid))
        {
            return queryGuid;
        }

        return null;
    }

    private static async Task<bool> ValidateTenantResourceAccess(ITenantContextService tenantContext, Guid resourceId)
    {
        // This is a placeholder - in a real implementation, you would validate
        // that the resource belongs to the current tenant by querying the database
        await Task.CompletedTask;
        return true; // For now, assume valid
    }
}

/// <summary>
/// Attribute for endpoints that require tenant admin privileges
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAdminAttribute : TenantValidationAttribute
{
    public RequireTenantAdminAttribute()
    {
        RequireTenant = true;
        ValidateResourceAccess = true;
    }
}

/// <summary>
/// Attribute for cross-tenant operations (system admin only)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CrossTenantOperationAttribute : Attribute, IAsyncActionFilter
{
    public string Operation { get; }

    public CrossTenantOperationAttribute(string operation)
    {
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userIdentity = context.HttpContext.RequestServices.GetRequiredService<ITenantUserIdentityService>();
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<CrossTenantOperationAttribute>>();

        try
        {
            if (!userIdentity.IsSystemAdmin())
            {
                logger.LogWarning("Non-system admin user {Email} attempted cross-tenant operation {Operation}",
                    userIdentity.GetCurrentUserEmail(), Operation);
                
                context.Result = new ObjectResult(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Forbidden",
                    Detail = "Cross-tenant operations require system administrator privileges",
                    Status = 403,
                    Instance = context.HttpContext.Request.Path
                })
                {
                    StatusCode = 403
                };
                return;
            }

            logger.LogDebug("System admin {Email} performing cross-tenant operation {Operation}",
                userIdentity.GetCurrentUserEmail(), Operation);

            await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during cross-tenant operation validation");
            
            context.Result = new ObjectResult(new ProblemDetails
            {
                Detail = "An error occurred during cross-tenant operation validation",
                Status = 500,
                Instance = context.HttpContext.Request.Path
            })
            {
                StatusCode = 500
            };
        }
    }
}