namespace RescueRanger.Api.Features.AppHealth.Health;

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string? Timestamp { get; set; }
}