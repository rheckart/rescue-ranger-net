using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api1.Middleware;
using RescueRanger.Core.Models;
using RescueRanger.Core.Repositories;
using RescueRanger.Core.Services;
using RescueRanger.Infrastructure.Data;
using RescueRanger.Infrastructure.Repositories;
using RescueRanger.Infrastructure.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

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
    
    // Add Entity Framework with PostgreSQL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptions => npgsqlOptions
                .MigrationsAssembly("RescueRanger.Infrastructure")
                .EnableRetryOnFailure()));

    // Add Health Checks
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var healthChecksBuilder = builder.Services.AddHealthChecks();
    
    // Only add PostgreSQL health check if connection string is available
    if (!string.IsNullOrEmpty(connectionString))
    {
        healthChecksBuilder.AddNpgSql(
            connectionString,
            name: "database",
            tags: ["db", "postgresql"]);
    }

    // Add CORS for development
    builder.Services.AddCors(options =>
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
    builder.Services.AddFastEndpoints()
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
    builder.Services.AddAuthorization();
    
    // Configure Multi-Tenant Options
    builder.Services.Configure<MultiTenantOptions>(builder.Configuration.GetSection("MultiTenant"));
    
    // Register tenant services
    builder.Services.AddScoped<ITenantContextService, TenantContextService>();
    builder.Services.AddScoped<ITenantRepository, TenantRepository>();
    builder.Services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
    builder.Services.AddScoped<ISubdomainTenantResolver, SubdomainTenantResolver>();
    
    var app = builder.Build();
    
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
            var userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();
            if (userAgent != null)
                diagnosticContext.Set("UserAgent", userAgent);
            
            // Add tenant context to logs
            var tenantService = httpContext.RequestServices.GetService<ITenantContextService>();
            if (tenantService is not { IsValid: true }) return;
            diagnosticContext.Set("TenantId", tenantService.TenantId);
            diagnosticContext.Set("TenantName", tenantService.TenantName);
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

// Make the implicit Program class public so test projects can access it
public partial class Program { }