namespace RescueRanger.Api.Features.AppHealth.ApiInfo;

public record Response(
    string Name,
    string Version,
    string Framework,
    string Environment,
    DateTime Timestamp
);