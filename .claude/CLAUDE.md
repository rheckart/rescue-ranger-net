# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Rescue Ranger is a comprehensive mobile-first SaaS application for horse rescue organizations, managing everything from individual horse care to volunteer coordination with offline capability and real-time synchronization.

## Technology Stack

### Backend
- **Framework**: .NET 9 with C# 13
- **API**: FastEndpoints NuGet package with ASP.NET Core Minimal API
- **Database**: PostgreSQL 17+ with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with JWT tokens, Google/Facebook OAuth, email magic links
- **Multi-tenancy**: Schema-based isolation in PostgreSQL
- **Primary Keys**: Vogen NuGet package for strongly-typed IDs
- **Error Handling**: Ardalis.Result NuGet package (avoid exceptions unless necessary)

### Frontend
- **Framework**: Vue.js 3 with Composition API
- **Build Tool**: Vite
- **UI Library**: Quasar.dev (installed via Quasar CLI)
- **CSS**: Quasar.dev styles with Sass/SCSS variables
- **Package Manager**: pnpm
- **Node Version**: 22 LTS
- **PWA**: Offline-first Progressive Web App

### Infrastructure
- **Hosting**: Digital Ocean App Platform
- **Database**: Digital Ocean Managed PostgreSQL
- **Assets**: Amazon S3 with CloudFront CDN
- **CI/CD**: GitHub Actions (main → production, staging → staging)
- **Repository**: https://github.com/rheckart/rescue-ranger-net

## Development Commands

### Backend (.NET)
```bash
# Project setup (when initialized)
dotnet new webapi -n RescueRanger.Api
dotnet new xunit -n RescueRanger.Tests
dotnet new sln -n RescueRanger
dotnet sln add RescueRanger.Api RescueRanger.Tests

# Development
dotnet build
dotnet run --project RescueRanger.Api
dotnet test
dotnet test --filter "FullyQualifiedName~TestClassName"
dotnet ef migrations add MigrationName
dotnet ef database update

# Code quality
dotnet format
```

### Frontend (Vue.js/Quasar)
```bash
# Project setup (when initialized)
pnpm create quasar
pnpm install

# Development
pnpm dev
pnpm build
pnpm test
pnpm test:unit -- TestFile.spec.ts
pnpm lint
pnpm format
```

## Architecture Patterns

### Backend Structure
- **FastEndpoints**:
  - IMPORTANT: Use the `Features` folder for FastEndpoint endpoints
  - IMPORTANT: Look at the `Features/AppHealth` folder and subfolders to understand how to structure endpoints, tests, and DTOs
  - IMPORTANT: Use Vertical Slice Architecture - Features should be organized by feature capability, not technical layers
- **Dependency Injection**: 
  - Use ASP.NET Core DI container for service registration
- **Repository Pattern**: 
  - Use Entity Framework Core repositories for CRUD operations
  - Respositories and other Entity Framework Core code should be kept in the `Data` folder
- **Unit of Work Pattern**: EF Core transactions for atomic operations
- **Fluent Validation**: Model validation with FluentValidation
  - IMPORTANT: Keep the validation code next to Features and DTOs as much as possible
- **Authorization**: ASP.NET Core Identity with role-based authorization
- **Multi-tenant Design**: Tenant resolution via subdomain or API header slug
- **Result Pattern**: Use Ardalis.Result for operation outcomes, not exceptions
- ** Primary ID Keys**:
  - Use Vogen for strongly-typed IDs
  - Prefer using version 7 GUIDs for primary keys

### Frontend Structure
- **Component Organization**: Features-based structure matching backend slices
- **State Management**: Pinia stores for global state
- **Offline Support**: Service workers with background sync for resilient operation
- **Role-Based UI**: Dynamic component rendering based on user roles

### Database Design
- **Schema Isolation**: Each tenant gets its own PostgreSQL schema
- **Audit Trail**: All entity changes tracked with user/timestamp
- **Soft Deletes**: Logical deletion for data recovery and compliance

## Key Domain Concepts

### User Roles Hierarchy
1. **President**: Full system access
2. **Board Member**: Full operational access
3. **Department Heads**: (Medical, Maintenance, Volunteer) - Department-specific access
4. **Feed Shift Lead/Co-Lead**: Shift management and critical operations
5. **Volunteer**: Task execution and basic reporting
6. **Event Coordinator**: Event management access

### Core Entities
- **Horse**: Central entity with medical history, care requirements, housing assignments
- **FeedShift**: Scheduled care sessions with task checklists
- **Medication**: Tracking administration with alerts
- **HealthRecord**: Vaccinations, deworming, dental, farrier care
- **User**: Multi-tenant aware with role assignments

### Critical Business Rules
- Temperature-based care decisions (blankets, fans, turnout)
- Medication administration restricted to leads
- Shift handover notes required between transitions
- Medical issues escalate to Head of Horse Welfare
- Offline changes sync when connection restored

## Development Phases

**Current Phase**: Pre-development (project initialization needed)

### Phase 1 (MVP) - In Planning
- Multi-tenant architecture with subdomain routing
- Authentication (OAuth + magic links)
- Basic horse profiles and role-based access
- Daily feed checklists with medication tracking

### Phase 2 - Enhanced Operations
- Complete shift management
- Weather-based care logic
- Medical records system
- Offline mode with sync

### Phase 3 - Advanced Features
- Document management
- Adoption workflows
- Analytics and reporting
- Third-party integrations

## Testing Strategy

### Backend
- Unit tests for all endpoints using xUnit
- Integration tests for database operations
- Multi-tenant isolation tests critical
- Authentication/authorization test coverage

### Frontend
- Component tests with Vitest
- E2E tests for critical user workflows
- Offline mode testing essential
- Cross-device responsiveness tests

## Security Considerations
- JWT tokens with refresh rotation
- Tenant isolation at database schema level
- Cloudflare Turnstile for signup protection
- Signed URLs for S3 document access
- Audit logging for compliance

## Performance Requirements
- Offline-first architecture for poor connectivity
- Sub-second response times for daily operations
- Support for 100+ concurrent users per tenant
- Optimized for mobile devices with limited resources

## Deployment Process
1. Push to `staging` branch triggers staging deployment
2. Manual testing in staging environment
3. PR to `main` branch with approval required
4. Merge to `main` triggers production deployment
5. Database migrations run automatically

## Common Troubleshooting

### Multi-tenancy Issues
- Verify tenant context in middleware
- Check schema isolation in EF Core queries
- Validate subdomain routing configuration

### Offline Sync Conflicts
- Last-write-wins for simple conflicts
- Manual resolution UI for complex conflicts
- Audit trail preserves all versions

### Performance Bottlenecks
- Index foreign keys and filter columns
- Implement pagination for large datasets
- Cache frequently accessed reference data
- Optimize images for mobile delivery