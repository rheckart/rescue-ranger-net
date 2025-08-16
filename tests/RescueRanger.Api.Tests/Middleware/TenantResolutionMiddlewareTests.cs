using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RescueRanger.Api1.Middleware;
using RescueRanger.Core.Entities;
using RescueRanger.Core.Enums;
using RescueRanger.Core.Models;
using RescueRanger.Core.Repositories;
using RescueRanger.Core.Services;
using RescueRanger.Core.ValueObjects;
using System.Text;
using Ardalis.Result;
using Xunit;
using FastEndpoints.Testing;
using FluentAssertions;

namespace RescueRanger.Api.Tests.Middleware;

public class TenantResolutionMiddlewareTests
{
    private readonly Mock<ITenantContextService> _mockTenantContextService;
    private readonly Mock<ITenantRepository> _mockTenantRepository;
    private readonly Mock<ILogger<TenantResolutionMiddleware>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly MultiTenantOptions _options;
    private readonly TenantResolutionMiddleware _middleware;
    private readonly RequestDelegate _next;
    private bool _nextCalled;
    
    public TenantResolutionMiddlewareTests()
    {
        _mockTenantContextService = new Mock<ITenantContextService>();
        _mockTenantRepository = new Mock<ITenantRepository>();
        _mockLogger = new Mock<ILogger<TenantResolutionMiddleware>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        
        _options = new MultiTenantOptions
        {
            BaseDomain = "rescueranger.com",
            EnableInDevelopment = true,
            DevelopmentTenant = "demo",
            ReservedSubdomains = new List<string> { "www", "api", "admin" }
        };
        
        _next = (HttpContext context) =>
        {
            _nextCalled = true;
            return Task.CompletedTask;
        };
        
        _middleware = new TenantResolutionMiddleware(
            _next,
            _mockLogger.Object,
            Options.Create(_options));
        
        _nextCalled = false;
    }
    
    [Fact]
    public async Task InvokeAsync_SkipsTenantResolution_ForHealthEndpoints()
    {
        // Arrange
        var context = CreateHttpContext("/health", "test.rescueranger.com");
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _nextCalled.Should().BeTrue();
        _mockTenantContextService.Verify(x => x.SetTenant(It.IsAny<TenantInfo>()), Times.Never);
    }
    
    [Fact]
    public async Task InvokeAsync_ResolvesFromSubdomain_Success()
    {
        // Arrange
        var tenant = CreateTestTenant("test", "Test Organization");
        var context = CreateHttpContext("/api/horses", "test.rescueranger.com");
        
        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync("test"))
            .ReturnsAsync(tenant);
        
        _mockTenantContextService
            .Setup(x => x.ValidateTenantAccessAsync())
            .ReturnsAsync(true);
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _nextCalled.Should().BeTrue();
        _mockTenantContextService.Verify(x => x.SetTenant(It.IsAny<TenantInfo>()), Times.Once);
        _mockTenantContextService.Verify(x => x.Clear(), Times.Once);
    }
    
    [Fact]
    public async Task InvokeAsync_ResolvesFromHeader_Success()
    {
        // Arrange
        var tenant = CreateTestTenant("headertest", "Header Test Org");
        var context = CreateHttpContext("/api/horses", "localhost");
        context.Request.Headers["X-Tenant-Subdomain"] = "headertest";
        
        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync("headertest"))
            .ReturnsAsync(tenant);
        
        _mockTenantContextService
            .Setup(x => x.ValidateTenantAccessAsync())
            .ReturnsAsync(true);
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _nextCalled.Should().BeTrue();
        _mockTenantContextService.Verify(x => x.SetTenant(It.IsAny<TenantInfo>()), Times.Once);
    }
    
    [Fact]
    public async Task InvokeAsync_ResolvesFromQueryParameter_Success()
    {
        // Arrange
        var tenant = CreateTestTenant("querytest", "Query Test Org");
        var context = CreateHttpContext("/api/horses?tenant=querytest", "localhost");
        
        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync("querytest"))
            .ReturnsAsync(tenant);
        
        _mockTenantContextService
            .Setup(x => x.ValidateTenantAccessAsync())
            .ReturnsAsync(true);
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _nextCalled.Should().BeTrue();
        _mockTenantContextService.Verify(x => x.SetTenant(It.IsAny<TenantInfo>()), Times.Once);
    }
    
    [Fact]
    public async Task InvokeAsync_Returns404_WhenTenantNotFound()
    {
        // Arrange
        var context = CreateHttpContext("/api/horses", "nonexistent.rescueranger.com");
        
        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync("nonexistent"))
            .ReturnsAsync(Result.NotFound());
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        
        var responseBody = await GetResponseBody(context);
        responseBody.Should().Be("Tenant not found");
    }
    
    [Fact]
    public async Task InvokeAsync_Returns403_WhenTenantAccessDenied()
    {
        // Arrange
        var tenant = CreateTestTenant("suspended", "Suspended Org", TenantStatus.Suspended);
        var context = CreateHttpContext("/api/horses", "suspended.rescueranger.com");
        
        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync("suspended"))
            .ReturnsAsync(tenant);
        
        _mockTenantContextService
            .Setup(x => x.ValidateTenantAccessAsync())
            .ReturnsAsync(false);
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        var responseBody = await GetResponseBody(context);
        responseBody.Should().Be("Tenant access denied");
    }
    
    [Fact]
    public async Task InvokeAsync_IgnoresReservedSubdomains()
    {
        // Arrange
        var context = CreateHttpContext("/api/horses", "www.rescueranger.com");
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _mockTenantRepository.Verify(x => x.GetBySubdomainAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task InvokeAsync_UsesDevelopmentTenant_InDevelopmentMode()
    {
        // Arrange
        var tenant = CreateTestTenant("demo", "Demo Organization");
        var context = CreateHttpContext("/api/horses", "localhost");
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");
        
        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync("demo"))
            .ReturnsAsync(tenant);
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _nextCalled.Should().BeTrue();
        _mockTenantContextService.Verify(x => x.SetTenant(It.IsAny<TenantInfo>()), Times.Once);
    }
    
    [Fact]
    public async Task InvokeAsync_ClearsTenantContext_OnException()
    {
        // Arrange
        var context = CreateHttpContext("/api/horses", "test.rescueranger.com");
        
        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        _mockTenantContextService.Verify(x => x.Clear(), Times.Exactly(2)); // Once in catch, once in finally
    }
    
    [Fact]
    public async Task InvokeAsync_ValidatesSubdomainFormat()
    {
        // Arrange
        var context = CreateHttpContext("/api/horses", "invalid_subdomain!.rescueranger.com");
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _mockTenantRepository.Verify(x => x.GetBySubdomainAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Theory]
    [InlineData("X-Tenant-Id", "tenant123")]
    [InlineData("X-Tenant-Subdomain", "customheader")]
    public async Task InvokeAsync_PrioritizesResolutionSources(string headerName, string headerValue)
    {
        // Arrange
        var tenant = CreateTestTenant(headerValue, "Test Org");
        var context = CreateHttpContext("/api/horses?tenant=queryvalue", "subdomain.rescueranger.com");
        context.Request.Headers[headerName] = headerValue;
        
        // Subdomain should take priority over header and query
        _mockTenantRepository
            .Setup(x => x.GetBySubdomainAsync("subdomain"))
            .ReturnsAsync(tenant);
        
        _mockTenantContextService
            .Setup(x => x.ValidateTenantAccessAsync())
            .ReturnsAsync(true);
        
        // Act
        await _middleware.InvokeAsync(context);
        
        // Assert
        _mockTenantRepository.Verify(x => x.GetBySubdomainAsync("subdomain"), Times.Once);
        _mockTenantRepository.Verify(x => x.GetBySubdomainAsync(headerValue), Times.Never);
        _mockTenantRepository.Verify(x => x.GetBySubdomainAsync("queryvalue"), Times.Never);
    }
    
    private HttpContext CreateHttpContext(string path, string host)
    {
        var context = new DefaultHttpContext();
        var services = new ServiceCollection();
        
        services.AddSingleton(_mockTenantContextService.Object);
        services.AddSingleton(_mockTenantRepository.Object);
        services.AddSingleton(_mockEnvironment.Object);
        
        context.RequestServices = services.BuildServiceProvider();
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Request.Host = new HostString(host);
        
        // Parse query string if present
        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            context.Request.Path = path[..queryIndex];
            context.Request.QueryString = new QueryString(path[queryIndex..]);
        }
        
        // Setup response body for capturing output
        context.Response.Body = new MemoryStream();
        
        return context;
    }
    
    private TenantInfo CreateTestTenant(string subdomain, string name, TenantStatus status = TenantStatus.Active)
    {
        return new TenantInfo
        {
            Id = Guid.NewGuid(),
            Name = name,
            Subdomain = subdomain,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            Configuration = new TenantConfiguration
            {
                MaxUsers = 10,
                MaxHorses = 100,
                StorageLimitMb = 1024,
                AdvancedFeaturesEnabled = false,
                Metadata = new Dictionary<string, string>()
            }
        };
    }
    
    private async Task<string> GetResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}