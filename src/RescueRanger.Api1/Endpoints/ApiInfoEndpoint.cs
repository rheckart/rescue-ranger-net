using FastEndpoints;
using System.Reflection;

namespace RescueRanger.Api1.Endpoints;

public class ApiInfoEndpoint : EndpointWithoutRequest<ApiInfoResponse>
{
    public override void Configure()
    {
        Get("/api/info");
        AllowAnonymous();
        Description(b => b
            .Produces<ApiInfoResponse>(200)
            .WithTags("System"));
        Summary(s =>
        {
            s.Summary = "Get API information";
            s.Description = "Returns metadata and version information about the API";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var framework = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName ?? "Unknown";
        
        await Send.OkAsync(new ApiInfoResponse(
            Name: "Rescue Ranger API",
            Version: version,
            Framework: framework,
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Timestamp: DateTime.UtcNow
        ), ct);
    }
}

public record ApiInfoResponse(
    string Name,
    string Version,
    string Framework,
    string Environment,
    DateTime Timestamp
);