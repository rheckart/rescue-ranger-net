namespace RescueRanger.Api.Features.AppHealth;

public record HealthCheckResponse(
    string Status,
    string Version,
    DateTime Timestamp
);

public record DetailedHealthCheckResponse(
    string Status,
    string Version,
    DateTime Timestamp,
    Dictionary<string, ComponentHealth> Components
);

public record ComponentHealth(
    string Status,
    string? Description = null,
    Dictionary<string, object>? Data = null
);