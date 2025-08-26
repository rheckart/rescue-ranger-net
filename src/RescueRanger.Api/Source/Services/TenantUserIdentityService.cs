using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Entities;
using RescueRanger.Api.Data;

namespace RescueRanger.Api.Services;

/// <summary>
/// Service for managing tenant-aware user identity
/// </summary>
public class TenantUserIdentityService : ITenantUserIdentityService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantContextService _tenantContext;
    private readonly ITenantAuthenticationService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantUserIdentityService> _logger;
    
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
    
    public TenantUserIdentityService(
        ApplicationDbContext context,
        ITenantContextService tenantContext,
        ITenantAuthenticationService authService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantUserIdentityService> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _authService = authService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    
    /// <inheritdoc />
    public async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue || !_tenantContext.IsValid)
            return null;
            
        try
        {
            // Get user within current tenant context
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive);
                
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user {UserId}", userId);
            return null;
        }
    }
    
    /// <inheritdoc />
    public Guid? GetCurrentUserId()
    {
        var userIdClaim = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
    
    /// <inheritdoc />
    public string? GetCurrentUserEmail()
    {
        return User?.FindFirstValue(ClaimTypes.Email);
    }
    
    /// <inheritdoc />
    public bool HasRole(string role)
    {
        return User?.IsInRole(role) == true;
    }
    
    /// <inheritdoc />
    public bool IsSystemAdmin()
    {
        var systemAdminClaim = User?.FindFirstValue("system_admin");
        return !string.IsNullOrEmpty(systemAdminClaim) && 
               bool.TryParse(systemAdminClaim, out var isAdmin) && isAdmin;
    }
    
    /// <inheritdoc />
    public bool CanSwitchTenant()
    {
        var canSwitchClaim = User?.FindFirstValue("can_switch_tenant");
        return !string.IsNullOrEmpty(canSwitchClaim) && 
               bool.TryParse(canSwitchClaim, out var canSwitch) && canSwitch;
    }
    
    /// <inheritdoc />
    public bool IsTenantSwitched()
    {
        var switchedClaim = User?.FindFirstValue("tenant_switched");
        return !string.IsNullOrEmpty(switchedClaim) && 
               bool.TryParse(switchedClaim, out var switched) && switched;
    }
    
    /// <inheritdoc />
    public Guid? GetOriginalTenantId()
    {
        var originalTenantClaim = User?.FindFirstValue("original_tenant_id");
        return Guid.TryParse(originalTenantClaim, out var tenantId) ? tenantId : null;
    }
    
    /// <inheritdoc />
    public async Task<bool> ValidateUserTenantAccessAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return false;
            
        return await _authService.ValidateUserTenantAccessAsync(userId.Value);
    }
    
    /// <inheritdoc />
    public ClaimsPrincipal CreateTenantAwarePrincipal(User user, TenantInfo tenant)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role),
            new("tenant_id", tenant.Id.ToString()),
            new("tenant_name", tenant.Name),
            new("tenant_subdomain", tenant.Subdomain),
            new("security_stamp", user.SecurityStamp),
            new("jti", Guid.NewGuid().ToString())
        };
        
        // Add system admin claims if applicable
        if (IsSystemAdminUser(user))
        {
            claims.Add(new Claim("system_admin", "true"));
            claims.Add(new Claim("can_switch_tenant", "true"));
        }
        
        var identity = new ClaimsIdentity(claims, "jwt");
        return new ClaimsPrincipal(identity);
    }
    
    /// <summary>
    /// Gets the full tenant user context
    /// </summary>
    /// <returns>Tenant user context if available</returns>
    public async Task<TenantUserContext?> GetTenantUserContextAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null || _tenantContext.CurrentTenant is null)
            return null;
            
        return new TenantUserContext
        {
            User = user,
            Tenant = _tenantContext.CurrentTenant,
            IsTenantSwitched = IsTenantSwitched(),
            OriginalTenantId = GetOriginalTenantId(),
            IsSystemAdmin = IsSystemAdmin(),
            CanSwitchTenant = CanSwitchTenant()
        };
    }
    
    private static bool IsSystemAdminUser(User user)
    {
        return user.Role.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase) ||
               user.Role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}