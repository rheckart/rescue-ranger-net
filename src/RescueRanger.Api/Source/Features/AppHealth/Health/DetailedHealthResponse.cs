namespace RescueRanger.Api.Features.AppHealth.Health;

public class DetailedHealthResponse : HealthResponse
{
    public string? Version { get; set; }
    public string? Environment { get; set; }
    public Dictionary<string, ServiceStatus>? Services { get; set; }
    public SystemInfo? System { get; set; }
}