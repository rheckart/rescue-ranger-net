using Amazon;
using Amazon.SimpleEmailV2;
using Dom;
using LettuceEncrypt;
using Microsoft.EntityFrameworkCore;
using RescueRanger.Api.Data.Repositories;
using RescueRanger.Api.Entities;
using RescueRanger.Api.Services;
using RescueRanger.Api1.Middleware;
using RescueRanger.Infrastructure.Data;
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
   .AddAuthorization()
   .AddFastEndpoints(o => o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All)
   .AddJobQueues<JobRecord, JobStorageProvider>()
   .AddSingleton<IAmazonSimpleEmailServiceV2>(
       new AmazonSimpleEmailServiceV2Client(
           bld.Configuration["Email:ApiKey"],
           bld.Configuration["Email:ApiSecret"],
           RegionEndpoint.USEast1));

// Add Serilog
bld.Host.UseSerilog();
    
// Add Entity Framework with PostgreSQL
bld.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        bld.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions
                         .MigrationsAssembly("RescueRanger.Api")
                         .EnableRetryOnFailure()));

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

// Add CORS for development
bld.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:9001")
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
    bld.Services.SwaggerDocument(d => d.DocumentSettings =
                                          s =>
                                          {
                                              s.DocumentName = "v0";
                                              s.Version = "0.0.0";
                                          });
}

// Configure Multi-Tenant Options
bld.Services.Configure<MultiTenantOptions>(bld.Configuration.GetSection("MultiTenant"));
    
// Register tenant services
bld.Services.AddScoped<ITenantContextService, TenantContextService>();
bld.Services.AddScoped<ITenantRepository, TenantRepository>();
bld.Services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
bld.Services.AddScoped<ISubdomainTenantResolver, SubdomainTenantResolver>();

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

if (!app.Environment.IsProduction())
    app.UseSwaggerGen(uiConfig: u => u.DeActivateTryItOut());

app.Run();

return 0;