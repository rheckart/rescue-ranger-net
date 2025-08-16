using System.Net;
using FastEndpoints.Testing;
using FluentAssertions;

namespace RescueRanger.Api.Tests;

public class HealthEndpointTests(RescueRangerApp app) : TestBase<RescueRangerApp>
{
    [Fact]
    public async Task BasicHealthEndpoint_ReturnsOk()
    {
        // Act
        var response = await app.Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Status");
        content.Should().Contain("Timestamp");
    }

    [Fact]
    public async Task ApiInfoEndpoint_ReturnsOk()
    {
        // Act
        var response = await app.Client.GetAsync("/api/info");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Rescue Ranger API");
        content.Should().Contain("Version");
        content.Should().Contain("Framework");
    }
}