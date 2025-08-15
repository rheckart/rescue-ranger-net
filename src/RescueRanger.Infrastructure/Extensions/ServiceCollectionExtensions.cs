using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RescueRanger.Core.Repositories;
using RescueRanger.Core.Services;
using RescueRanger.Infrastructure.Repositories;
using RescueRanger.Infrastructure.Services;

namespace RescueRanger.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register multi-tenant services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds multi-tenant services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMultiTenant(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Redis caching
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "RescueRanger";
        });
        
        // Register tenant services
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
        services.AddScoped<ISubdomainTenantResolver, SubdomainTenantResolver>();
        
        return services;
    }
    
    /// <summary>
    /// Adds multi-tenant services without Redis caching (uses in-memory cache)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddMultiTenantInMemory(this IServiceCollection services)
    {
        // Add in-memory caching for development
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        
        // Register tenant services
        services.AddScoped<ITenantContextService, TenantContextService>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantResolver, SubdomainTenantResolver>();
        services.AddScoped<ISubdomainTenantResolver, SubdomainTenantResolver>();
        
        return services;
    }
}