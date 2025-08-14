using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RescueRanger.Core.Entities;
using RescueRanger.Core.Interfaces;
using RescueRanger.Core.Services;

namespace RescueRanger.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantContextService? _tenantContext;
    private readonly string? _currentUser;
    
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContextService? tenantContext = null)
        : base(options)
    {
        _tenantContext = tenantContext;
        // In a real app, this would come from IHttpContextAccessor or similar
        _currentUser = _tenantContext?.TenantSubdomain ?? "System";
    }
    
    // DbSets
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Horse> Horses => Set<Horse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Apply global query filters for multi-tenancy
        ApplyGlobalQueryFilters(modelBuilder);
    }
    
    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Find all entity types that implement ITenantEntity
        var tenantEntityTypes = modelBuilder.Model
            .GetEntityTypes()
            .Where(t => t.ClrType.GetInterfaces().Contains(typeof(ITenantEntity)))
            .ToList();
        
        foreach (var entityType in tenantEntityTypes)
        {
            // Create filter expression: e => e.TenantId == _tenantContext.TenantId
            var method = typeof(ApplicationDbContext)
                .GetMethod(nameof(SetTenantQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)?
                .MakeGenericMethod(entityType.ClrType);
            
            method?.Invoke(this, new object[] { modelBuilder });
        }
    }
    
    private void SetTenantQueryFilter<T>(ModelBuilder modelBuilder) where T : class, ITenantEntity
    {
        if (_tenantContext != null)
        {
            Expression<Func<T, bool>> filterExpression = entity => entity.TenantId == _tenantContext.TenantId;
            modelBuilder.Entity<T>().HasQueryFilter(filterExpression);
        }
    }
    
    public override int SaveChanges()
    {
        OnBeforeSaving();
        return base.SaveChanges();
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        OnBeforeSaving();
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    private void OnBeforeSaving()
    {
        var entries = ChangeTracker.Entries();
        var utcNow = DateTime.UtcNow;
        
        foreach (var entry in entries)
        {
            // Set TenantId for new tenant entities
            if (entry.Entity is ITenantEntity tenantEntity && entry.State == EntityState.Added)
            {
                if (_tenantContext?.IsValid == true && tenantEntity.TenantId == Guid.Empty)
                {
                    tenantEntity.TenantId = _tenantContext.TenantId;
                }
            }
            
            // Set audit fields
            if (entry.Entity is BaseEntity baseEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        baseEntity.CreatedAt = utcNow;
                        baseEntity.CreatedBy = _currentUser ?? "System";
                        break;
                    
                    case EntityState.Modified:
                        baseEntity.UpdatedAt = utcNow;
                        baseEntity.UpdatedBy = _currentUser ?? "System";
                        // Don't modify the CreatedAt/CreatedBy
                        entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Temporarily bypass tenant filter for administrative operations
    /// </summary>
    public IQueryable<T> AllTenants<T>() where T : class
    {
        return Set<T>().IgnoreQueryFilters();
    }
}