using System.ComponentModel.DataAnnotations;
using System.Net;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.Auth.Login;

/// <summary>
/// Login request
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User password
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to remember this login
    /// </summary>
    public bool RememberMe { get; set; }
}

/// <summary>
/// Login response
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// Access token
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Refresh token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Token expiration
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// User information
    /// </summary>
    public LoginUserInfo User { get; set; } = null!;
    
    /// <summary>
    /// Tenant information
    /// </summary>
    public LoginTenantInfo Tenant { get; set; } = null!;
}

/// <summary>
/// User information for login response
/// </summary>
public class LoginUserInfo
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
    /// Full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether user is system admin
    /// </summary>
    public bool IsSystemAdmin { get; set; }
    
    /// <summary>
    /// Whether user can switch tenants
    /// </summary>
    public bool CanSwitchTenant { get; set; }
}

/// <summary>
/// Tenant information for login response
/// </summary>
public class LoginTenantInfo
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
/// Endpoint for user authentication within tenant context
/// </summary>
public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly ITenantAuthenticationService _authService;
    private readonly ITenantContextService _tenantContext;
    private readonly ILogger<LoginEndpoint> _logger;
    
    public LoginEndpoint(
        ITenantAuthenticationService authService,
        ITenantContextService tenantContext,
        ILogger<LoginEndpoint> logger)
    {
        _authService = authService;
        _tenantContext = tenantContext;
        _logger = logger;
    }
    
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Summary(s => 
        {
            s.Summary = "Authenticate user within tenant context";
            s.Description = "Authenticates a user and returns JWT tokens with tenant claims";
            s.Responses[200] = "Authentication successful";
            s.Responses[400] = "Invalid request";
            s.Responses[401] = "Invalid credentials";
            s.Responses[403] = "Account locked or deactivated";
        });
    }
    
    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        try
        {
            if (!_tenantContext.IsValid)
            {
                AddError("Tenant context not established");
                await SendAsync(HttpStatusCode.BadRequest, ct);
                return;
            }
            
            var result = await _authService.AuthenticateAsync(req.Email, req.Password);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Authentication failed for {Email} in tenant {TenantId}: {Errors}",
                    req.Email, _tenantContext.TenantId, string.Join(", ", result.Errors));
                    
                var statusCode = result.Status switch
                {
                    Ardalis.Result.ResultStatus.NotFound => HttpStatusCode.Unauthorized,
                    Ardalis.Result.ResultStatus.Forbidden => HttpStatusCode.Forbidden,
                    _ => HttpStatusCode.Unauthorized
                };
                
                AddError("Authentication failed");
                await SendAsync(statusCode, ct);
                return;
            }
            
            var authResult = result.Value;
            var isSystemAdmin = IsSystemAdmin(authResult.User);
            
            var response = new LoginResponse
            {
                AccessToken = authResult.AccessToken,
                RefreshToken = authResult.RefreshToken,
                ExpiresAt = authResult.TokenExpiresAt,
                User = new LoginUserInfo
                {
                    Id = authResult.User.Id,
                    Email = authResult.User.Email,
                    FullName = authResult.User.FullName,
                    Role = authResult.User.Role,
                    IsSystemAdmin = isSystemAdmin,
                    CanSwitchTenant = isSystemAdmin
                },
                Tenant = new LoginTenantInfo
                {
                    Id = authResult.Tenant.Id,
                    Name = authResult.Tenant.Name,
                    Subdomain = authResult.Tenant.Subdomain
                }
            };
            
            _logger.LogInformation("User {Email} authenticated successfully in tenant {TenantName}",
                authResult.User.Email, authResult.Tenant.Name);
            
            Response = response;
            await Send.OkAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for {Email}", req.Email);
            AddError("Authentication failed");
            await SendAsync(HttpStatusCode.InternalServerError, ct);
        }
    }
    
    private static bool IsSystemAdmin(RescueRanger.Api.Entities.User user)
    {
        return user.Role.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase) ||
               user.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}