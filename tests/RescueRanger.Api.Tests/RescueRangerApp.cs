using FastEndpoints.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RescueRanger.Infrastructure.Data;

namespace RescueRanger.Api.Tests;

public class RescueRangerApp : AppFixture<Program>
{
    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Override configuration for tests
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=TestDb;Username=test;Password=test;",
                ["MultiTenant:Enabled"] = "false",
                ["Serilog:MinimumLevel:Default"] = "Warning"
            });
        });
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Remove the real database
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
        if (descriptor != null)
            services.Remove(descriptor);
        
        // Add in-memory database for testing
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDatabase_{Guid.NewGuid()}");
        });
    }
}