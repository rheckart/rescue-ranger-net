using Amazon;
using Amazon.SimpleEmailV2;
using LettuceEncrypt;
using RescueRanger.Api.Authorization;
using RescueRanger.Api.Data.Repositories;
using RescueRanger.Api.HealthChecks;
using RescueRanger.Api.Middleware;
using RescueRanger.Api.Services;
using RescueRanger.Api1.Middleware;
using RescueRanger.Infrastructure.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Xunit.Runner.InProc.SystemConsole;

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

if (args.Contains("@@")) // this is a 'dotnet test' run
    return await ConsoleRunner.Run(args);

var bld = WebApplication.CreateBuilder(args);
bld.Services
   .AddAuthenticationJwtBearer(o => o.SigningKey = bld.Configuration["Auth:SigningKey"])
   .AddTenantAuthorization() // Add tenant-aware authorization
   .AddFastEndpoints(o => o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All)
   .AddJobQueues<JobRecord, JobStorageProvider>()
   .AddSingleton<IAmazonSimpleEmailServiceV2>(
       new AmazonSimpleEmailServiceV2Client(
           bld.Configuration["Email:ApiKey"],
           bld.Configuration["Email:ApiSecret"],
           RegionEndpoint.USEast1));

// Add HttpContextAccessor for accessing HTTP context in services
bld.Services.AddHttpContextAccessor();

// Add Serilog
bld.Host.UseSerilog();
    
// Add Entity Framework with PostgreSQL
bld.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        bld.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions
                         .MigrationsAssembly("RescueRanger.Api")
                         .EnableRetryOnFailure()));

// Add Redis distributed caching
var redisConnectionString = bld.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    bld.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "RescueRanger";
    });
}
else
{
    // Fallback for dev/local so IDistributedCache is always available
    bld.Services.AddDistributedMemoryCache();
}

// Add Health Checks
var connectionString = bld.Configuration.GetConnectionString("DefaultConnection");
var healthChecksBuilder = bld.Services.AddHealthChecks();

// Only add PostgreSQL health check if connection string is available
if (!string.IsNullOrEmpty(connectionString))
{
    healthChecksBuilder.AddNpgSql(
        connectionString,
        name: "database",
        tags: ["db", "postgresql"]);
}

// Add Redis health check only if connection string is available
if (!string.IsNullOrEmpty(redisConnectionString))
{
    healthChecksBuilder.AddRedis(
        redisConnectionString,
        name: "redis",
        tags: ["cache", "redis"]);
}

// Add tenant resolution health check
healthChecksBuilder.AddTypeActivatedCheck<TenantResolutionHealthCheck>(
    "tenant-resolution");

// Add additional tenant-specific health checks
healthChecksBuilder.AddTypeActivatedCheck<TenantSpecificHealthCheck>(
    "tenant-specific");
healthChecksBuilder.AddTypeActivatedCheck<TenantConfigurationHealthCheck>(
    "tenant-configuration");
healthChecksBuilder.AddTypeActivatedCheck<TenantResolutionPerformanceHealthCheck>(
    "tenant-resolution-performance");

// Add CORS for development and multi-tenant support
bld.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:9001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    // Multi-tenant CORS policy for subdomains
    options.AddPolicy("MultiTenantPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
              {
                  var uri = new Uri(origin);
                  var host = uri.Host;
                  
                  // Allow localhost for development
                  if (host == "localhost" || host == "127.0.0.1")
                      return true;
                  
                  // Get base domain from configuration
                  var config = bld.Configuration.GetSection("MultiTenant");
                  var baseDomain = config["BaseDomain"] ?? "rescueranger.com";
                  
                  // Allow any subdomain of the base domain
                  return host.EndsWith($".{baseDomain}", StringComparison.OrdinalIgnoreCase) ||
                         host.Equals(baseDomain, StringComparison.OrdinalIgnoreCase);
              })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

if (bld.Environment.IsProduction())
{
    bld.Services
       .AddLettuceEncrypt()
       .PersistDataToDirectory(new("/home/RescueRanger.Api/certs"), null);
}
else
{
    bld.Services.SwaggerDocument(d => 
    {
        d.DocumentSettings = s =>
        {
            s.DocumentName = "v1";
            s.Version = "1.0.0";
            s.Title = "RescueRanger Multi-Tenant API";
            s.Description = @"
# RescueRanger Multi-Tenant API

This API supports multi-tenancy through subdomain-based tenant resolution.

## Authentication & Tenant Context

All API requests must include proper authentication and tenant context:

1. **Tenant Resolution**: The tenant is automatically resolved from the subdomain
   - Format: `https://{tenant}.rescueranger.com/api/endpoint`
   - Example: `https://happytails.rescueranger.com/api/horses`

2. **Authentication**: Include JWT token in Authorization header
   - Format: `Authorization: Bearer {jwt_token}`

3. **Cross-Tenant Access**: Prevented automatically - users can only access data from their tenant

## Security Features

- **Automatic Tenant Filtering**: All queries are automatically filtered by tenant
- **Cross-Tenant Protection**: Prevents accidental or malicious cross-tenant access
- **Audit Logging**: All tenant access is logged for security monitoring
- **Role-Based Authorization**: Different access levels within tenants
";
        };
    });
}

// Configure Multi-Tenant Options
bld.Services.Configure<MultiTenantOptions>(bld.Configuration.GetSection("MultiTenant"));

// Register tenant resolution metrics
bld.Services.AddSingleton<TenantResolutionMetrics>();
    
// Register tenant services
bld.Services.AddScoped<ITenantContextService, TenantContextService>();
bld.Services.AddScoped<ITenantRepository, TenantRepository>();
bld.Services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
bld.Services.AddScoped<ISubdomainTenantResolver, SubdomainTenantResolver>();
bld.Services.AddScoped<ITenantService, TenantService>();

// Register authentication and user services
bld.Services.AddScoped<IJwtTokenService, JwtTokenService>();
bld.Services.AddScoped<ITenantAuthenticationService, TenantAuthenticationService>();
bld.Services.AddScoped<ITenantUserIdentityService, TenantUserIdentityService>();
bld.Services.AddScoped<ITenantUserValidationService, TenantUserValidationService>();

// Register audit and security services
bld.Services.AddScoped<ITenantAuditService, TenantAuditService>();

// Register tenant-aware repositories
bld.Services.AddScoped(typeof(ITenantAwareRepository<>), typeof(TenantAwareRepository<>));
bld.Services.AddScoped<HorseRepository>();
bld.Services.AddScoped<MemberRepository>();

var app = bld.Build();

app.UseAuthentication()
   .UseAuthorization()
   .UseFastEndpoints(c =>
   {
       c.Errors.UseProblemDetails();
   });

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
        if (userAgent is not null)
            diagnosticContext.Set("UserAgent", userAgent);
            
        // Add tenant context to logs
        var tenantService = httpContext.RequestServices.GetService<ITenantContextService>();

        if (tenantService is not { IsValid: true })
            return;

        diagnosticContext.Set("TenantId", tenantService.TenantId);
        diagnosticContext.Set("TenantName", tenantService.TenantName);
    };
});
    
// Add Tenant Resolution Middleware (before authentication/authorization)
app.UseTenantResolution();

// Add Tenant Context Validation Middleware
app.UseTenantContextValidation();

// Add Tenant Token Validation Middleware (after authentication, before authorization)
app.UseTenantTokenValidation();

app.UseJobQueues(o =>
{
    o.MaxConcurrency = 4;
    o.ExecutionTimeLimit = TimeSpan.FromSeconds(20);
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseCors("DevelopmentPolicy");
}
else
{
    // Use multi-tenant CORS policy in production
    app.UseCors("MultiTenantPolicy");
}

if (!app.Environment.IsProduction())
    app.UseSwaggerGen(uiConfig: u => u.DeActivateTryItOut());

app.Run();

return 0;