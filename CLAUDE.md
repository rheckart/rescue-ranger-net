# Rescue Ranger - Horse Rescue Management System

This is a comprehensive SaaS solution built with .NET 9 and Vue.js/Quasar for managing horse rescue operations.

## Project Overview

**Technology Stack:**
- **Backend**: .NET 9 with C# 13, Clean Architecture
- **API Framework**: FastEndpoints for minimal API development  
- **Database**: PostgreSQL with Entity Framework Core
- **Caching**: Redis
- **Logging**: Serilog with structured JSON logging
- **Frontend**: Vue.js 3 with Composition API + TypeScript
- **UI Framework**: Quasar Framework for mobile-first design
- **State Management**: Pinia
- **Build Tools**: Vite + ESBuild
- **IDE**: JetBrains Rider (recommended)

## Quick Start

### Development Environment Setup

1. **Prerequisites:**
   - .NET 9 SDK
   - Node.js 20+
   - pnpm (preferred) or npm
   - Docker & Docker Compose
   - JetBrains Rider (recommended)

2. **Start Development Environment:**
   ```bash
   # Option 1: JetBrains Rider (Recommended)
   # Open RescueRanger.sln in Rider
   # Select "Full Stack - API + Frontend" run configuration
   # Click Run button
   
   # Option 2: Manual startup
   docker-compose up postgres redis -d
   cd src/RescueRanger.Api && dotnet run  # Terminal 1
   cd rescue-ranger-client && pnpm dev    # Terminal 2
   
   # Option 3: Full Docker environment
   docker-compose --profile full-stack up --build
   ```

3. **Access Points:**
   - Frontend: http://localhost:9000
   - API: http://localhost:5000
   - API Documentation: http://localhost:5000/swagger
   - Health Check: http://localhost:5000/health

## Project Structure

### Backend (.NET 9 Solution)
```
src/
├── RescueRanger.Api/           # Web API & FastEndpoints
│   ├── Endpoints/              # API endpoint definitions
│   ├── Program.cs              # Application startup
│   └── Dockerfile              # Container configuration
├── RescueRanger.Core/          # Domain layer (entities, interfaces)
├── RescueRanger.Infrastructure/ # Data access & external services
└── RescueRanger.Contracts/     # API contracts & DTOs
```

### Frontend (Quasar/Vue.js)
```
rescue-ranger-client/
├── src/
│   ├── components/            # Reusable Vue components
│   ├── layouts/              # Page layouts
│   ├── pages/                # Route components
│   ├── services/             # API service layer
│   ├── stores/               # Pinia state management
│   └── router/               # Vue Router configuration
├── quasar.config.ts          # Quasar framework configuration
└── Dockerfile.dev            # Development container
```

## Development Workflow

### Backend Development
- **Architecture**: Clean Architecture with separation of concerns
- **Endpoints**: FastEndpoints for minimal APIs with automatic OpenAPI generation
- **Database**: PostgreSQL with EF Core migrations
- **Logging**: Structured JSON logging with Serilog
- **Health Checks**: Custom health endpoints at `/health` and `/health/ready`

### Frontend Development  
- **Framework**: Quasar with Vue.js 3 Composition API
- **TypeScript**: Full TypeScript support with strict mode
- **API Integration**: Axios with proxy configuration for development
- **State Management**: Pinia for reactive state
- **Hot Reload**: HMR enabled for fast development

### Database Management
```bash
# Run migrations
cd src/RescueRanger.Api
dotnet ef database update

# Add new migration
dotnet ef migrations add <MigrationName>
```

## JetBrains Rider Integration

**Available Run Configurations:**
- **RescueRanger.API**: Backend only (port 5000)
- **Quasar Frontend**: Frontend only (port 9000)
- **Full Stack - API + Frontend**: Both services simultaneously
- **Docker Compose - Development**: Complete containerized environment

## Key Features Implemented

### Health Check System
- **Basic Health**: `/health` - Simple API status
- **Detailed Health**: `/health/ready` - Comprehensive service status
- **Frontend Integration**: Health check page with service monitoring
- **Database Connectivity**: PostgreSQL connection verification

### Development Tools
- **Docker Compose**: Multi-service development environment
- **API Documentation**: Swagger/OpenAPI at `/swagger`
- **CORS Configuration**: Enabled for localhost:9000 in development
- **Proxy Setup**: Frontend API requests proxy to backend

## Architecture Decisions

### Backend Architecture
- **Clean Architecture**: Separation of concerns with domain-driven design
- **FastEndpoints**: Chosen over controllers for better performance and testability
- **PostgreSQL**: Robust relational database for horse rescue data
- **Serilog**: Structured logging for better observability

### Frontend Architecture  
- **Quasar Framework**: Material Design with mobile-first approach
- **Composition API**: Vue 3 Composition API for better TypeScript support
- **Pinia**: Modern state management replacing Vuex
- **TypeScript**: Strict typing for better developer experience

## Common Development Commands

```bash
# Backend
dotnet build                    # Build solution
dotnet run --project src/RescueRanger.Api  # Run API
dotnet test                     # Run tests

# Frontend  
pnpm install                    # Install dependencies
pnpm dev                        # Start development server
pnpm build                      # Build for production
pnpm lint                       # Run ESLint

# Docker
docker-compose up postgres redis -d  # Start services only
docker-compose --profile full-stack up  # Start everything
```

## Next Steps

1. **Entity Models**: Implement horse, volunteer, and rescue operation entities
2. **Authentication**: Add JWT-based authentication and authorization
3. **CRUD Operations**: Implement core business logic endpoints
4. **Frontend Pages**: Build management dashboards and forms
5. **File Upload**: Add photo and document management
6. **Notifications**: Implement real-time updates
7. **Reporting**: Add analytics and reporting features

## Troubleshooting

### Port Conflicts
- API default: 5000 (configurable in `launchSettings.json`)
- Frontend default: 9000 (configurable in `quasar.config.ts`)

### Database Issues
- Ensure PostgreSQL container is running: `docker-compose ps`
- Check connection string in `appsettings.Development.json`
- Verify network connectivity between containers

### Frontend Build Issues
- Clear dependencies: `rm -rf node_modules && pnpm install`
- Check Node.js version: `node --version` (requires 20+)
- Verify proxy configuration in `quasar.config.ts`

## References

- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/core/)
- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [Quasar Framework](https://quasar.dev/)
- [Vue.js 3 Guide](https://vuejs.org/guide/)
- [Pinia State Management](https://pinia.vuejs.org/)