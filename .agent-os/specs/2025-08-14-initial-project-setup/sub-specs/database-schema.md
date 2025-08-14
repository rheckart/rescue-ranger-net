# Database Schema - Initial Project Setup

## Overview

Initial database setup with connection configuration only. No tables or migrations are created in this phase - only the database connection and Entity Framework Core configuration.

## Database Configuration

### PostgreSQL Setup
```sql
-- Database creation (handled by Docker Compose)
CREATE DATABASE rescueranger_dev;
CREATE USER rescueranger WITH PASSWORD 'development';
GRANT ALL PRIVILEGES ON DATABASE rescueranger_dev TO rescueranger;
```

### Connection String
```
Host=localhost;Port=5432;Database=rescueranger_dev;Username=rescueranger;Password=development
```

## Entity Framework Core Configuration

### DbContext Setup
```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Future: Multi-tenant schema configuration
        // modelBuilder.HasDefaultSchema("public");
    }
}
```

### Service Registration
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions
            .MigrationsAssembly("RescueRanger.Infrastructure")
            .EnableRetryOnFailure()
    ));
```

## Health Check Configuration

### Database Health Check
```csharp
services.AddHealthChecks()
    .AddNpgSql(
        configuration.GetConnectionString("DefaultConnection"),
        name: "database",
        tags: new[] { "db", "postgresql" });
```

## Migration Strategy

### Development Environment
- Migrations will be created as features are implemented
- Initial migration will be empty (no tables)
- Command: `dotnet ef migrations add InitialCreate`

### Migration Commands Reference
```bash
# Add migration
dotnet ef migrations add MigrationName --project src/RescueRanger.Infrastructure --startup-project src/RescueRanger.Api

# Update database
dotnet ef database update --project src/RescueRanger.Infrastructure --startup-project src/RescueRanger.Api

# Remove last migration
dotnet ef migrations remove --project src/RescueRanger.Infrastructure --startup-project src/RescueRanger.Api
```

## Future Schema Considerations

### Multi-Tenancy Approach
- Schema-based isolation per tenant
- Each tenant gets their own PostgreSQL schema
- Shared `public` schema for system tables

### Planned Schema Structure (Future)
```
Database: rescueranger_dev
├── public/                 # System tables
│   ├── tenants
│   └── system_settings
├── tenant_001/            # First tenant schema
│   ├── horses
│   ├── users
│   ├── feed_shifts
│   └── ...
└── tenant_002/            # Second tenant schema
    ├── horses
    ├── users
    ├── feed_shifts
    └── ...
```

### Base Entity Pattern (Future)
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
```

## Connection Pooling

### Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=rescueranger_dev;Username=rescueranger;Password=development;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;"
  }
}
```

## Backup Strategy (Development)

### Docker Volume Backup
```bash
# Backup
docker run --rm -v rescue-ranger-net_postgres_data:/data -v $(pwd):/backup alpine tar czf /backup/postgres-backup.tar.gz -C /data .

# Restore
docker run --rm -v rescue-ranger-net_postgres_data:/data -v $(pwd):/backup alpine tar xzf /backup/postgres-backup.tar.gz -C /data
```

## Performance Indexes (Future)

Planned indexes for future implementation:
- Foreign key indexes (automatic with EF Core conventions)
- Composite indexes for multi-tenant queries
- Full-text search indexes for horse names/descriptions
- Temporal indexes for scheduling queries

## Security Considerations

### Development Only Settings
- Plain text password in connection string (development only)
- Trust authentication for local Docker
- No SSL/TLS encryption required locally

### Production Considerations (Future)
- Azure Key Vault for connection strings
- SSL/TLS required for connections
- Row-level security for multi-tenancy
- Encrypted columns for sensitive data