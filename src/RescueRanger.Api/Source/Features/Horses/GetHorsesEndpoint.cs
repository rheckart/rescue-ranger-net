using RescueRanger.Api.Authorization;
using RescueRanger.Api.Data.Repositories;
using RescueRanger.Api.Entities;
using RescueRanger.Api.Services;

namespace RescueRanger.Api.Features.Horses;

/// <summary>
/// Endpoint for retrieving horses - demonstrates comprehensive tenant security
/// </summary>
[TenantValidation(RequireTenant = true, ValidateResourceAccess = true)]
public sealed class GetHorsesEndpoint : Endpoint<GetHorsesRequest, GetHorsesResponse>
{
    private readonly HorseRepository _horseRepository;
    private readonly ITenantAuditService _auditService;
    private readonly ITenantContextService _tenantContext;
    private readonly ILogger<GetHorsesEndpoint> _logger;

    public GetHorsesEndpoint(
        HorseRepository horseRepository,
        ITenantAuditService auditService,
        ITenantContextService tenantContext,
        ILogger<GetHorsesEndpoint> logger)
    {
        _horseRepository = horseRepository;
        _auditService = auditService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/horses");
        Summary(s =>
        {
            s.Summary = "Get horses for the current tenant";
            s.Description = "Retrieves a list of horses belonging to the current tenant with optional filtering and pagination";
            s.Responses[200] = "Horses retrieved successfully";
            s.Responses[403] = "Forbidden - cross-tenant access denied";
            s.Responses[404] = "Tenant not found";
        });

        // Require authentication and valid tenant membership
        Policies(TenantAuthorizationPolicies.TenantUser);
    }

    public override async Task HandleAsync(GetHorsesRequest req, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Retrieving horses for tenant {TenantId} with filters: Status={Status}, Search={Search}",
                _tenantContext.TenantId, req.Status, req.Search);

            // Use tenant-aware repository (automatically filters by tenant)
            var horses = req.Status switch
            {
                not null => await _horseRepository.GetByStatusAsync(req.Status, ct),
                _ => await _horseRepository.GetAllAsync(ct)
            };

            // Apply additional filtering if needed
            if (!string.IsNullOrWhiteSpace(req.Search))
            {
                horses = horses.Where(h => 
                    h.Name.Contains(req.Search, StringComparison.OrdinalIgnoreCase) ||
                    (h.Breed?.Contains(req.Search, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            // Apply pagination
            var totalCount = horses.Count;
            var pagedHorses = horses
                .Skip((req.Page - 1) * req.PageSize)
                .Take(req.PageSize)
                .ToList();

            var response = new GetHorsesResponse
            {
                Horses = pagedHorses.Select(MapHorseToDto).ToList(),
                TotalCount = totalCount,
                Page = req.Page,
                PageSize = req.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / req.PageSize)
            };

            // Log successful access for audit
            await _auditService.LogTenantAccessAsync(new TenantAccessEvent
            {
                TenantId = _tenantContext.TenantId,
                UserId = HttpContext.RequestServices.GetRequiredService<ITenantUserIdentityService>().GetCurrentUserId(),
                UserEmail = HttpContext.RequestServices.GetRequiredService<ITenantUserIdentityService>().GetCurrentUserEmail(),
                RequestId = HttpContext.TraceIdentifier,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                Endpoint = "/horses",
                HttpMethod = "GET",
                StatusCode = 200,
                ResponseTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                AdditionalData = new Dictionary<string, object>
                {
                    ["horses_count"] = pagedHorses.Count,
                    ["total_count"] = totalCount,
                    ["status_filter"] = req.Status ?? "all",
                    ["search_term"] = req.Search ?? "",
                    ["page"] = req.Page
                }
            });

            await Send.OkAsync(response, ct);

            _logger.LogInformation("Successfully retrieved {Count} horses for tenant {TenantId}",
                pagedHorses.Count, _tenantContext.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving horses for tenant {TenantId}", _tenantContext.TenantId);
            
            await Send.StringAsync("An error occurred while retrieving horses", 500, cancellation: ct);
        }
    }

    private static HorseDto MapHorseToDto(Horse horse)
    {
        return new HorseDto
        {
            Id = horse.Id,
            Name = horse.Name,
            Breed = horse.Breed,
            Age = horse.Age,
            Color = horse.Color,
            Gender = horse.Gender,
            Status = horse.Status,
            IsAvailableForAdoption = horse.IsAvailableForAdoption,
            AdoptionFee = horse.AdoptionFee,
            PhotoUrls = horse.PhotoUrls,
            CurrentLocation = horse.CurrentLocation
        };
    }
}

/// <summary>
/// Request model for getting horses
/// </summary>
public sealed class GetHorsesRequest
{
    /// <summary>
    /// Filter by status (optional)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Search term for name or breed (optional)
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Page number (default: 1)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (default: 20, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Response model for getting horses
/// </summary>
public sealed class GetHorsesResponse
{
    /// <summary>
    /// List of horses for the current page
    /// </summary>
    public List<HorseDto> Horses { get; set; } = new();

    /// <summary>
    /// Total number of horses (across all pages)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// DTO for horse information
/// </summary>
public sealed class HorseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public int? Age { get; set; }
    public string? Color { get; set; }
    public string? Gender { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsAvailableForAdoption { get; set; }
    public decimal? AdoptionFee { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public string? CurrentLocation { get; set; }
}