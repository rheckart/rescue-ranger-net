using System.Diagnostics;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Middleware;

/// <summary>
/// Middleware for comprehensive tenant context validation and audit logging
/// </summary>
public class TenantContextValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantContextValidationMiddleware> _logger;

    public TenantContextValidationMiddleware(RequestDelegate next, ILogger<TenantContextValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantContext = context.RequestServices.GetService<ITenantContextService>();
        var userIdentity = context.RequestServices.GetService<ITenantUserIdentityService>();

        if (tenantContext == null || userIdentity == null)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        var userEmail = userIdentity.GetCurrentUserEmail();
        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;

        try
        {
            // Log tenant access attempt
            if (tenantContext.IsValid)
            {
                _logger.LogDebug("Tenant context validated - TenantId: {TenantId}, User: {UserEmail}, RequestId: {RequestId}",
                    tenantContext.TenantId, userEmail, requestId);

                // Add tenant information to response headers for debugging (only in non-production)
                if (!context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction())
                {
                    context.Response.Headers["X-Tenant-Id"] = tenantContext.TenantId.ToString();
                    context.Response.Headers["X-Tenant-Name"] = tenantContext.TenantName;
                }

                // Validate tenant access for authenticated users
                if (isAuthenticated && !userIdentity.IsSystemAdmin())
                {
                    var hasAccess = await userIdentity.ValidateUserTenantAccessAsync();
                    if (!hasAccess)
                    {
                        _logger.LogWarning("Cross-tenant access attempt blocked - User: {UserEmail}, TargetTenant: {TenantId}, RequestId: {RequestId}, Path: {Path}",
                            userEmail, tenantContext.TenantId, requestId, context.Request.Path);

                        await WriteUnauthorizedResponse(context, "Cross-tenant access denied");
                        return;
                    }
                }

                // Log successful tenant validation
                _logger.LogInformation("Tenant access validated - TenantId: {TenantId}, User: {UserEmail}, RequestId: {RequestId}, Path: {Path}",
                    tenantContext.TenantId, userEmail, requestId, context.Request.Path);
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant context validation - RequestId: {RequestId}, TenantId: {TenantId}",
                requestId, tenantContext.TenantId);

            await WriteErrorResponse(context, "Internal server error during tenant validation");
        }
        finally
        {
            stopwatch.Stop();
            
            // Log performance metrics
            _logger.LogDebug("Tenant validation completed - Duration: {Duration}ms, RequestId: {RequestId}",
                stopwatch.ElapsedMilliseconds, requestId);
        }
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            title = "Forbidden",
            status = 403,
            detail = message,
            instance = context.Request.Path.ToString()
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(problem));
    }

    private static async Task WriteErrorResponse(HttpContext context, string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = 500,
            detail = message,
            instance = context.Request.Path.ToString()
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(problem));
    }
}

/// <summary>
/// Extension methods for registering tenant context validation middleware
/// </summary>
public static class TenantContextValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds tenant context validation middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseTenantContextValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantContextValidationMiddleware>();
    }
}