using System.ComponentModel.DataAnnotations;
using System.Net;
using RescueRanger.Api.Authorization;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.Auth.SwitchTenant;

/// <summary>
/// Request to switch tenant context
/// </summary>
public class SwitchTenantRequest
{
    /// <summary>
    /// Target tenant ID to switch to
    /// </summary>
    [Required]
    public Guid TargetTenantId { get; set; }
}

/// <summary>
/// Response for tenant switching
/// </summary>
public class SwitchTenantResponse
{
    /// <summary>
    /// New access token for target tenant
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// New refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Target tenant information
    /// </summary>
    public TenantSwitchInfo TargetTenant { get; set; } = null!;
    
    /// <summary>
    /// Original tenant information
    /// </summary>
    public TenantSwitchInfo OriginalTenant { get; set; } = null!;
    
    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// User information
    /// </summary>
    public UserSwitchInfo User { get; set; } = null!;
}

/// <summary>
/// Tenant information for switch response
/// </summary>
public class TenantSwitchInfo
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Tenant name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tenant subdomain
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;
}

/// <summary>
/// User information for switch response
/// </summary>
public class UserSwitchInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// User email
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether user is system admin
    /// </summary>
    public bool IsSystemAdmin { get; set; }
}

/// <summary>
/// Endpoint for tenant switching (system admins only)
/// </summary>
public class SwitchTenantEndpoint : Endpoint<SwitchTenantRequest, SwitchTenantResponse>
{
    private readonly ITenantAuthenticationService _authService;
    private readonly ITenantUserIdentityService _userIdentity;
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantService _tenantService;
    private readonly ILogger<SwitchTenantEndpoint> _logger;
    
    public SwitchTenantEndpoint(
        ITenantAuthenticationService authService,
        ITenantUserIdentityService userIdentity,
        ITenantContextService tenantContext,
        ITenantService tenantService,
        ILogger<SwitchTenantEndpoint> logger)
    {
        _authService = authService;
        _userIdentity = userIdentity;
        _tenantContext = tenantContext;
        _tenantService = tenantService;
        _logger = logger;
    }
    
    public override void Configure()
    {
        Post("/auth/switch-tenant");
        Policies(TenantAuthorizationPolicies.CrossTenantAccess);
        Summary(s =>
        {
            s.Summary = "Switch to a different tenant context";
            s.Description = "Allows system administrators to switch their session to a different tenant";
            s.Response(200, "Tenant switched successfully");
            s.Response(403, "Insufficient permissions");
            s.Response(404, "Target tenant not found");
        });
    }
    
    public override async Task HandleAsync(SwitchTenantRequest req, CancellationToken ct)
    {
        try
        {
            var currentUserId = _userIdentity.GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                await Send.UnauthorizedAsync(ct);
                return;
            }
            
            // Verify user is system admin
            if (!_userIdentity.IsSystemAdmin())
            {
                await Send.ForbiddenAsync(ct);
                return;
            }
            
            // Get original tenant info
            var originalTenant = _tenantContext.CurrentTenant;
            if (originalTenant is null)
            {
                await Send.StringAsync("Current tenant context not established", 400, cancellation: ct);
                return;
            }
            
            // Switch tenant
            var switchResult = await _authService.SwitchTenantAsync(currentUserId.Value, req.TargetTenantId);
            if (!switchResult.IsSuccess)
            {
                await Send.StringAsync(string.Join(", ", switchResult.Errors), 400, cancellation: ct);
                return;
            }
            
            var authResult = switchResult.Value;
            
            // Get target tenant info
            var targetTenant = await _tenantService.GetTenantInfoAsync(req.TargetTenantId);
            if (targetTenant is null)
            {
                AddError("Target tenant not found");
                await Send.NotFoundAsync(ct);
                return;
            }
            
            var response = new SwitchTenantResponse
            {
                AccessToken = authResult.AccessToken,
                RefreshToken = authResult.RefreshToken,
                ExpiresAt = authResult.TokenExpiresAt,
                TargetTenant = new TenantSwitchInfo
                {
                    Id = targetTenant.Id,
                    Name = targetTenant.Name,
                    Subdomain = targetTenant.Subdomain
                },
                OriginalTenant = new TenantSwitchInfo
                {
                    Id = originalTenant.Id,
                    Name = originalTenant.Name,
                    Subdomain = originalTenant.Subdomain
                },
                User = new UserSwitchInfo
                {
                    Id = authResult.User.Id,
                    Email = authResult.User.Email,
                    FullName = authResult.User.FullName,
                    IsSystemAdmin = true
                }
            };
            
            _logger.LogInformation("System admin {Email} switched from tenant {OriginalTenant} to {TargetTenant}",
                authResult.User.Email, originalTenant.Id, targetTenant.Id);
            
            await Send.OkAsync(response, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching to tenant {TargetTenantId}", req.TargetTenantId);
            await Send.StringAsync("An error occurred while switching tenants", 500, cancellation: ct);
        }
    }
}