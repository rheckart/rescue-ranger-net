using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Middleware;
using RescueRanger.Core.Models;
using RescueRanger.Core.Repositories;
using RescueRanger.Core.Services;
using RescueRanger.Infrastructure.Data;
using RescueRanger.Infrastructure.Repositories;
using RescueRanger.Infrastructure.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace RescueRanger.Api;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(new JsonFormatter())
            .WriteTo.File(
                new JsonFormatter(),
                "logs/api-.json",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting Rescue Ranger API");
            
            var builder = WebApplication.CreateBuilder(args);
            
            // Add Serilog
            builder.Host.UseSerilog();
            
            // Add services to the container
            ConfigureServices(builder.Services, builder.Configuration);
            
            var app = builder.Build();
            
            // Configure the HTTP request pipeline
            ConfigureApp(app);
            
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add Entity Framework with PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions
                    .MigrationsAssembly("RescueRanger.Infrastructure")
                    .EnableRetryOnFailure()));

        // Add Health Checks
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection") ?? "",
                name: "database",
                tags: new[] { "db", "postgresql" });

        // Add CORS for development
        services.AddCors(options =>
        {
            options.AddPolicy("DevelopmentPolicy", policy =>
            {
                policy.WithOrigins("http://localhost:9001")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // Add FastEndpoints
        services.AddFastEndpoints()
                .SwaggerDocument(o =>
                {
                    o.DocumentSettings = s =>
                    {
                        s.Title = "Rescue Ranger API";
                        s.Version = "v1.0";
                        s.Description = "API for managing horse rescue operations";
                    };
                });

        // Add Authorization (placeholder for future implementation)
        services.AddAuthorization();
        
        // Configure Multi-Tenant Options
        services.Configure<MultiTenantOptions>(configuration.GetSection("MultiTenant"));
        
        // Register tenant services
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
        services.AddScoped<ISubdomainTenantResolver, SubdomainTenantResolver>();
    }

    private static void ConfigureApp(WebApplication app)
    {
        // Add security headers
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            await next();
        });

        // Serilog Request Logging
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
                var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
                if (userAgent != null)
                    diagnosticContext.Set("UserAgent", userAgent);
                
                // Add tenant context to logs
                var tenantService = httpContext.RequestServices.GetService<ITenantContextService>();
                if (tenantService != null && tenantService.IsValid)
                {
                    diagnosticContext.Set("TenantId", tenantService.TenantId);
                    diagnosticContext.Set("TenantName", tenantService.TenantName);
                }
            };
        });
        
        // Add Tenant Resolution Middleware (before authentication/authorization)
        app.UseTenantResolution();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseCors("DevelopmentPolicy");
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.MapGet("/error", () => Results.Problem());
        }

        // Authorization
        app.UseAuthorization();

        // FastEndpoints middleware
        app.UseFastEndpoints()
           .UseSwaggerGen();

        // Custom health check endpoints are handled by FastEndpoints
    }
}