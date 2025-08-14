# Rescue Ranger - Horse Rescue Management System

A comprehensive SaaS solution built with .NET 9 and Vue.js/Quasar for managing horse rescue operations.

## 🚀 Quick Start

### Prerequisites

- .NET 9 SDK
- Node.js 20+
- pnpm (preferred) or npm
- Docker & Docker Compose
- JetBrains Rider (recommended)

### Development Setup

1. **Clone and navigate to the project:**
   ```bash
   git clone <repository-url>
   cd rescue-ranger-net
   ```

2. **Start the development environment:**

   **Option 1: JetBrains Rider (Recommended)**
   - Open `RescueRanger.sln` in Rider
   - Select "Full Stack - API + Frontend" run configuration
   - Click Run button

   **Option 2: Manual startup**
   ```bash
   # Terminal 1: Start PostgreSQL (if not running)
   docker run -d -p 5432:5432 -e POSTGRES_DB=rescueranger_dev -e POSTGRES_USER=rescueranger -e POSTGRES_PASSWORD=development postgres:16
   
   # Terminal 2: Start API
   cd src/RescueRanger.Api && dotnet run
   
   # Terminal 3: Start Frontend
   cd rescue-ranger-client && quasar dev
   ```

3. **Access the application:**
   - Frontend: http://localhost:9000
   - API: http://localhost:5000
   - API Documentation: http://localhost:5000/swagger
   - Health Check: http://localhost:5000/health

## 🏗️ Architecture

### Backend (.NET 9)
- **Clean Architecture** with clear separation of concerns
- **FastEndpoints** for minimal API development
- **PostgreSQL** with Entity Framework Core
- **Serilog** for structured JSON logging
- **Health Checks** for system monitoring

### Frontend (Vue.js 3 + Quasar)
- **Vue 3 Composition API** with TypeScript
- **Quasar Framework** for mobile-first UI
- **Pinia** for state management
- **Vite** for fast development

## 📁 Project Structure

```
rescue-ranger-net/
├── src/
│   ├── RescueRanger.Api/           # Web API & FastEndpoints
│   ├── RescueRanger.Core/          # Domain layer
│   ├── RescueRanger.Infrastructure/ # Data access & external services
│   └── RescueRanger.Contracts/     # API contracts & DTOs
├── rescue-ranger-client/           # Vue.js/Quasar frontend
├── tests/                          # Test projects
├── .run/                          # JetBrains Rider configurations
└── docker-compose.yml            # Development infrastructure
```

## 🔧 Development

### Backend Commands
```bash
# Build solution
dotnet build

# Run API
cd src/RescueRanger.Api && dotnet run

# Run tests
dotnet test

# Add migration
cd src/RescueRanger.Api && dotnet ef migrations add <MigrationName>

# Update database
cd src/RescueRanger.Api && dotnet ef database update
```

### Frontend Commands
```bash
cd rescue-ranger-client

# Install dependencies
pnpm install

# Start development server
quasar dev

# Build for production
quasar build

# Run linting
pnpm lint
```

## 🩺 Health Monitoring

The application includes comprehensive health checks:

- **Basic Health:** `/health` - Simple API status
- **Detailed Health:** `/health/ready` - Includes database connectivity, system info
- **API Info:** `/api/info` - Version and environment information

## 🔄 Available Run Configurations

The project includes JetBrains Rider run configurations:

- **RescueRanger.API** - Backend only
- **Quasar Frontend** - Frontend only  
- **Full Stack - API + Frontend** - Both services simultaneously

## 🛠️ Technology Stack

| Component | Technology |
|-----------|------------|
| Backend Framework | .NET 9 with C# 13 |
| API Framework | FastEndpoints |
| Database | PostgreSQL 16 |
| ORM | Entity Framework Core 9 |
| Logging | Serilog (JSON) |
| Frontend Framework | Vue.js 3 + TypeScript |
| UI Library | Quasar Framework |
| State Management | Pinia |
| Build Tool | Vite |

## ✅ Verification Checklist

After setup, verify everything is working:

- [ ] API responds at http://localhost:5000/health
- [ ] Frontend loads at http://localhost:9000  
- [ ] Swagger UI available at http://localhost:5000/swagger
- [ ] PostgreSQL connection verified through detailed health check
- [ ] Frontend can call API endpoints (test via health check page)
- [ ] CORS is configured for localhost:9000

## 🚨 Troubleshooting

### Port Conflicts
- API default: 5000 (configure in `launchSettings.json`)
- Frontend default: 9000 (configure in `quasar.config.ts`)

### Database Issues
- Ensure PostgreSQL is running on port 5432
- Check connection string in `appsettings.Development.json`
- Verify credentials: `rescueranger`/`development`

### Frontend Build Issues
- Clear dependencies: `rm -rf node_modules && pnpm install`
- Verify Node.js version: `node --version` (requires 20+)
- Check proxy configuration in `quasar.config.ts`

## 📚 Documentation

- [.NET 9 Documentation](https://docs.microsoft.com/en-us/dotnet/core/)
- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [Quasar Framework](https://quasar.dev/)
- [Vue.js 3 Guide](https://vuejs.org/guide/)
- [Pinia State Management](https://pinia.vuejs.org/)

## 📝 Next Steps

1. **Entity Models** - Implement horse, volunteer, and rescue operation entities
2. **Authentication** - Add JWT-based authentication and authorization  
3. **CRUD Operations** - Implement core business logic endpoints
4. **Frontend Pages** - Build management dashboards and forms
5. **File Upload** - Add photo and document management
6. **Notifications** - Implement real-time updates
7. **Reporting** - Add analytics and reporting features