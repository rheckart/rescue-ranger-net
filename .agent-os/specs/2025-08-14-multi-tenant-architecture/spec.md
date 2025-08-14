# Multi-Tenant Architecture with Subdomain Routing Specification

## Overview

Implement a comprehensive multi-tenant SaaS architecture for the Rescue Ranger horse rescue management system that enables multiple horse rescue organizations to operate on a single platform with complete data isolation and subdomain-based tenant identification. Each organization will have their own subdomain (e.g., `mysticacres.rescueranger.com`, `wildhorserescue.rescueranger.com`) with fully isolated data and configurations while sharing the same underlying infrastructure and codebase.

This foundation will support hundreds of concurrent rescue organizations with automatic tenant resolution, robust data isolation, and scalable architecture patterns that enable rapid tenant provisioning and management.

## User Stories

### Story 1: Tenant Administrator Onboarding
**As a** new rescue organization administrator  
**I want to** register my organization and receive a custom subdomain  
**So that I can** access my organization's dedicated rescue management portal

**Workflow:**
1. Visit the main registration page at `rescueranger.com/register`
2. Fill out organization details including preferred subdomain
3. Receive confirmation and activation email
4. Access the system at `[subdomain].rescueranger.com`
5. Complete organization setup and begin managing rescue operations

### Story 2: Data Isolation and Security
**As a** rescue organization administrator  
**I want to** be certain that my organization's data is completely isolated  
**So that I can** trust the system with sensitive animal and volunteer information

**Workflow:**
1. Log in to my organization's subdomain
2. Create horses, volunteers, and rescue records
3. Verify that no data from other organizations is visible
4. Confirm that all database queries are automatically filtered to my tenant
5. Trust that the system maintains complete data security

### Story 3: System Administrator Tenant Management
**As a** system administrator  
**I want to** efficiently manage tenant organizations and monitor system health  
**So that I can** ensure optimal platform performance and security

**Workflow:**
1. Access the admin portal at `admin.rescueranger.com`
2. View all tenant organizations and their status
3. Provision new tenants or suspend existing ones
4. Monitor tenant resource usage and performance metrics
5. Perform system-wide maintenance without affecting individual tenants

## Spec Scope

### Core Multi-Tenancy Components
1. **Tenant Resolution Middleware**: Automatically extract tenant information from subdomain and inject into request context
2. **Database Multi-Tenancy**: Shared database schema with tenant isolation via TenantId column and global query filters
3. **Tenant Context Service**: Centralized tenant information management throughout the application lifecycle
4. **Tenant Provisioning System**: Automated tenant creation and configuration workflow
5. **Admin Management Portal**: System-wide tenant administration and monitoring tools

### Security and Data Isolation
1. **Global Query Filters**: Entity Framework configuration ensuring automatic tenant filtering on all database operations
2. **Tenant-Aware Authentication**: JWT tokens with tenant context and validation
3. **Cross-Tenant Access Prevention**: Middleware and validation to prevent unauthorized tenant access
4. **Audit Logging**: Comprehensive logging of tenant operations and security events

### Infrastructure and Scalability
1. **Subdomain Routing**: Both API and frontend routing based on subdomain extraction
2. **Tenant Configuration Storage**: Flexible tenant-specific settings and feature flags
3. **Performance Optimization**: Efficient tenant resolution with caching and database indexing
4. **Health Monitoring**: Tenant-specific health checks and performance metrics

## Out of Scope

- Domain registration and DNS management (assumed to be handled externally)
- Payment processing and billing systems
- Advanced tenant analytics and reporting
- Cross-tenant data sharing or federation features
- Multi-region deployment and data residency
- Tenant-specific UI customization and branding

## Technical Approach

### Database Strategy
**Shared Database with Tenant Isolation**
- Single PostgreSQL database with all tenant data
- Every multi-tenant entity includes a `TenantId` column
- Entity Framework Core global query filters ensure automatic tenant isolation
- Database indexes on TenantId for optimal query performance
- Tenant metadata stored in dedicated `Tenants` table

### Tenant Identification Flow
1. **Subdomain Extraction**: Middleware extracts subdomain from incoming HTTP requests
2. **Tenant Resolution**: Cache-first lookup of tenant configuration from database
3. **Context Injection**: Tenant information injected into dependency injection container
4. **Request Processing**: All subsequent operations are automatically tenant-aware

### Entity Framework Configuration
```csharp
// Global query filter example
modelBuilder.Entity<Horse>()
    .HasQueryFilter(h => h.TenantId == CurrentTenantId);

// Automatic TenantId injection on SaveChanges
protected override int SaveChanges()
{
    foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.TenantId = _tenantContext.TenantId;
        }
    }
    return base.SaveChanges();
}
```

## Architecture Components

### 1. Tenant Resolution Middleware
**Location**: `src/RescueRanger.Infrastructure/Middleware/TenantResolutionMiddleware.cs`

**Responsibilities**:
- Extract subdomain from HTTP request host header
- Resolve tenant configuration from database or cache
- Inject tenant context into DI container for request scope
- Handle tenant not found scenarios gracefully
- Log tenant resolution events for monitoring

**Key Features**:
- Redis caching for tenant lookup performance
- Fallback handling for invalid or non-existent tenants
- Configurable tenant resolution strategies
- Health check integration for tenant status

### 2. Tenant Context Service
**Location**: `src/RescueRanger.Core/Services/ITenantContextService.cs`

**Responsibilities**:
- Provide current tenant information throughout the application
- Validate tenant access permissions
- Manage tenant-specific configuration and settings
- Handle tenant switching scenarios (admin contexts)

**Interface Design**:
```csharp
public interface ITenantContextService
{
    TenantInfo CurrentTenant { get; }
    Guid TenantId { get; }
    string TenantSubdomain { get; }
    bool IsValidTenant { get; }
    Task<TenantConfiguration> GetTenantConfigurationAsync();
    Task<bool> ValidateTenantAccessAsync(Guid userId);
}
```

### 3. Multi-Tenant Entity Framework Configuration
**Location**: `src/RescueRanger.Infrastructure/Data/MultiTenantDbContext.cs`

**Features**:
- Automatic TenantId injection on entity creation
- Global query filters for all tenant-aware entities
- Tenant-specific connection string support (future enhancement)
- Audit trail with tenant information
- Migration support for multi-tenant schema

### 4. Tenant Management Endpoints
**Location**: `src/RescueRanger.Api/Endpoints/Admin/TenantManagementEndpoints.cs`

**Endpoints**:
- `POST /admin/tenants` - Create new tenant
- `GET /admin/tenants` - List all tenants with pagination
- `GET /admin/tenants/{id}` - Get tenant details
- `PUT /admin/tenants/{id}` - Update tenant configuration
- `DELETE /admin/tenants/{id}` - Suspend/deactivate tenant
- `POST /admin/tenants/{id}/activate` - Reactivate tenant

### 5. Frontend Tenant Routing
**Location**: `rescue-ranger-client/src/router/tenantRouter.ts`

**Features**:
- Automatic subdomain detection in browser
- Tenant-specific route configuration
- Redirect handling for invalid tenants
- Admin portal routing separation
- Tenant context injection into Vue stores

## Data Model

### Core Tenant Entity
```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public TenantConfiguration Configuration { get; set; } = new();
    
    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Horse> Horses { get; set; } = new List<Horse>();
}

public enum TenantStatus
{
    Pending,
    Active,
    Suspended,
    Canceled
}
```

### Multi-Tenant Base Entity
```csharp
public abstract class TenantEntity : ITenantEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    // Standard entity properties
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

### Tenant Configuration
```csharp
public class TenantConfiguration
{
    public int MaxUsers { get; set; } = 50;
    public int MaxHorses { get; set; } = 500;
    public bool EnableNotifications { get; set; } = true;
    public bool EnableReporting { get; set; } = true;
    public Dictionary<string, object> CustomSettings { get; set; } = new();
    public List<string> EnabledFeatures { get; set; } = new();
}
```

### Example Multi-Tenant Entities
```csharp
public class Horse : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public HorseStatus Status { get; set; }
    // Additional horse properties...
}

public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<UserRole> Roles { get; set; }
    // Additional user properties...
}
```

## Security Considerations

### Data Isolation
1. **Database Level**: Every query automatically filtered by TenantId through EF Core global filters
2. **Application Level**: Tenant context validation on every request
3. **API Level**: Authorization policies that verify tenant access
4. **Frontend Level**: Tenant-aware state management and API calls

### Cross-Tenant Security
1. **URL Manipulation Protection**: Validate tenant context matches authenticated user
2. **Direct Object Reference**: All entity access validated against current tenant
3. **Administrative Access**: Super admin context with explicit tenant switching audit logs
4. **JWT Token Security**: Tenant ID embedded in claims and validated on each request

### Audit and Monitoring
1. **Security Events**: Log all tenant access attempts and failures
2. **Data Access Logs**: Track cross-tenant data access attempts
3. **Performance Monitoring**: Tenant-specific performance metrics and alerts
4. **Compliance Logging**: Detailed audit trails for regulatory requirements

## Implementation Tasks

### Phase 1: Core Infrastructure (Week 1)
1. **Database Schema**: Create tenant tables and multi-tenant base entities
2. **Tenant Context**: Implement tenant context service and dependency injection
3. **Middleware**: Create tenant resolution middleware with subdomain parsing
4. **EF Configuration**: Setup global query filters and automatic TenantId injection

### Phase 2: API Integration (Week 1-2)
1. **Tenant Endpoints**: Implement tenant management API endpoints
2. **Authentication**: Integrate tenant context with JWT authentication
3. **Authorization**: Create tenant-aware authorization policies
4. **Health Checks**: Add tenant-specific health monitoring

### Phase 3: Frontend Integration (Week 2)
1. **Subdomain Detection**: Implement client-side tenant resolution
2. **Router Configuration**: Setup tenant-aware routing
3. **State Management**: Integrate tenant context with Pinia stores
4. **API Client**: Update axios configuration for tenant headers

### Phase 4: Admin Portal (Week 2-3)
1. **Admin UI**: Create tenant management interface
2. **Tenant Provisioning**: Implement automated tenant creation workflow
3. **Monitoring Dashboard**: Build tenant status and metrics display
4. **Bulk Operations**: Add batch tenant management capabilities

### Phase 5: Testing and Optimization (Week 3)
1. **Security Testing**: Comprehensive cross-tenant access testing
2. **Performance Testing**: Load testing with multiple tenants
3. **Integration Testing**: End-to-end multi-tenant workflow testing
4. **Documentation**: Complete technical documentation and runbooks

## Testing Strategy

### Unit Tests
- Tenant context service functionality
- Middleware tenant resolution logic
- Global query filter effectiveness
- Entity Framework tenant isolation

### Integration Tests
- End-to-end tenant registration workflow
- Cross-tenant data access prevention
- API endpoint tenant isolation
- Database query filter validation

### Security Tests
- SQL injection attempts with tenant bypass
- Direct object reference with cross-tenant access
- JWT token manipulation for tenant switching
- URL manipulation for unauthorized tenant access

### Performance Tests
- Tenant resolution latency under load
- Database query performance with tenant filters
- Concurrent tenant operations scaling
- Cache effectiveness for tenant lookups

## Success Metrics

### Security Metrics
- **Zero Cross-Tenant Data Leaks**: No data visible across tenant boundaries
- **100% Query Filter Coverage**: All tenant-aware entities properly filtered
- **Sub-100ms Tenant Resolution**: Fast tenant context resolution
- **99.9% Uptime Per Tenant**: High availability for individual tenants

### Performance Metrics
- **<50ms Average Tenant Resolution**: Quick subdomain-to-tenant mapping
- **Linear Scaling**: Performance degrades linearly with tenant count
- **<5% Query Overhead**: Minimal performance impact from tenant filtering
- **1000+ Concurrent Tenants**: Support for large-scale multi-tenancy

### Operational Metrics
- **<1 Minute Tenant Provisioning**: Fast new tenant setup
- **Zero Manual Tenant Configuration**: Fully automated tenant creation
- **100% Tenant Health Visibility**: Complete monitoring and alerting
- **<24 Hour Issue Resolution**: Fast support for tenant-specific issues

## Expected Deliverable

1. **Fully Functional Multi-Tenant Platform**: Complete tenant isolation with subdomain routing working for both API and frontend applications

2. **Secure Data Isolation**: Zero possibility of cross-tenant data access with comprehensive testing validation and monitoring in place

3. **Automated Tenant Management**: Self-service tenant provisioning with admin portal for tenant lifecycle management and monitoring

4. **Production-Ready Architecture**: Scalable, maintainable, and well-documented multi-tenant foundation ready for business logic implementation

5. **Comprehensive Testing Suite**: Full test coverage including security, performance, and integration tests validating multi-tenant functionality

The implementation will provide a robust foundation for the Rescue Ranger SaaS platform, enabling rapid onboarding of new rescue organizations while maintaining strict data security and optimal performance characteristics.