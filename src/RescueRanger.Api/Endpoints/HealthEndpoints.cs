namespace RescueRanger.Api.Endpoints;

public class BasicHealthEndpoint(HealthCheckService healthCheckService) : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Basic health check";
            s.Description = "Returns basic health status of the API";
            s.Responses[200] = "API is healthy";
            s.Responses[503] = "API is unhealthy";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var health = await healthCheckService.CheckHealthAsync(ct);
        
        var response = new HealthResponse
        {
            Status = health.Status.ToString(),
            Timestamp = DateTimeOffset.UtcNow.ToString("O")
        };

        if (health.Status == HealthStatus.Healthy)
        {
            await Send.OkAsync(response, ct);
        }
        else
        {
            await Send.ResultAsync(Results.StatusCode(503));
        }
    }
}

public class DetailedHealthEndpoint(HealthCheckService healthCheckService)
    : EndpointWithoutRequest<DetailedHealthResponse>
{
    public override void Configure()
    {
        Get("/health/ready");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Detailed health check";
            s.Description = "Returns detailed health status including services and system information";
            s.Responses[200] = "API and all services are healthy";
            s.Responses[503] = "API or one or more services are unhealthy";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var health = await healthCheckService.CheckHealthAsync(ct);
        var process = System.Diagnostics.Process.GetCurrentProcess();
        
        var response = new DetailedHealthResponse
        {
            Status = health.Status.ToString(),
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Services = health.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new ServiceStatus
                {
                    Status = kvp.Value.Status.ToString(),
                    ResponseTime = kvp.Value.Duration.TotalMilliseconds.ToString("F2") + "ms",
                    Error = kvp.Value.Exception?.Message
                }
            ),
            System = new SystemInfo
            {
                Uptime = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).ToString(@"dd\.hh\:mm\:ss"),
                MemoryUsage = $"{GC.GetTotalMemory(false) / 1024 / 1024} MB"
            }
        };

        if (health.Status == HealthStatus.Healthy)
        {
            await Send.OkAsync(response, ct);
        }
        else
        {
            await Send.ResultAsync(Results.StatusCode(503));
        }
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string? Timestamp { get; set; }
}

public class DetailedHealthResponse : HealthResponse
{
    public string? Version { get; set; }
    public string? Environment { get; set; }
    public Dictionary<string, ServiceStatus>? Services { get; set; }
    public SystemInfo? System { get; set; }
}

public class ServiceStatus
{
    public string Status { get; set; } = string.Empty;
    public string? ResponseTime { get; set; }
    public string? Error { get; set; }
}

public class SystemInfo
{
    public string Uptime { get; set; } = string.Empty;
    public string MemoryUsage { get; set; } = string.Empty;
}