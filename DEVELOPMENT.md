# Development Environment Setup

This guide explains how to set up and run the Rescue Ranger application in development mode.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/) 
- [pnpm](https://pnpm.io/) (recommended) or npm
- [Docker](https://www.docker.com/) (for database)
- [JetBrains Rider](https://www.jetbrains.com/rider/) (recommended IDE)

## Quick Start

### Option 1: Using JetBrains Rider (Recommended)

1. Open the solution file `RescueRanger.sln` in JetBrains Rider
2. Select the **"Full Stack - API + Frontend"** run configuration from the dropdown
3. Click the Run button to start both API and frontend simultaneously

Available run configurations:
- **RescueRanger.API**: Backend API only (port 5000)
- **Quasar Frontend**: Frontend only (port 9000)  
- **Full Stack - API + Frontend**: Both backend and frontend
- **Docker Compose - Development**: Full containerized environment

### Option 2: Manual Setup

1. **Start Database Services**
   ```bash
   docker-compose up postgres redis -d
   ```

2. **Start Backend API**
   ```bash
   cd src/RescueRanger.Api
   dotnet run
   ```
   API will be available at http://localhost:5000

3. **Start Frontend** (in a new terminal)
   ```bash
   cd rescue-ranger-client
   pnpm install
   pnpm dev
   ```
   Frontend will be available at http://localhost:9000

### Option 3: Full Docker Environment

```bash
docker-compose --profile full-stack up --build
```

## Development Workflow

### Backend Development

- API runs on http://localhost:5000
- Swagger UI available at http://localhost:5000/swagger
- Health checks at http://localhost:5000/health
- Hot reload enabled in development mode

### Frontend Development

- Frontend runs on http://localhost:9000
- Hot module replacement (HMR) enabled
- API proxy configured for `/api` requests → `http://localhost:5000`
- TypeScript compilation and linting on save

### Database Management

- PostgreSQL runs on localhost:5432
- Database: `rescue_ranger`
- Username: `rescue_ranger`
- Password: `dev_password_123`

Run migrations:
```bash
cd src/RescueRanger.Api
dotnet ef database update
```

## Project Structure

```
rescue-ranger-net/
├── src/                          # Backend .NET solution
│   ├── RescueRanger.Api/         # Web API project
│   ├── RescueRanger.Core/        # Domain layer
│   ├── RescueRanger.Infrastructure/ # Data access layer
│   └── RescueRanger.Contracts/   # API contracts
├── rescue-ranger-client/         # Frontend Quasar project
├── tests/                        # Unit and integration tests
├── docker-compose.yml           # Development services
└── .idea/runConfigurations/     # JetBrains Rider run configs
```

## Key Endpoints

- **Frontend**: http://localhost:9000
- **API**: http://localhost:5000
- **API Documentation**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health
- **Database**: localhost:5432
- **Redis**: localhost:6379

## Troubleshooting

### Port Conflicts
- API: Change port in `launchSettings.json` and `quasar.config.ts` proxy
- Frontend: Change port in `quasar.config.ts` devServer configuration

### Database Connection Issues
- Ensure PostgreSQL container is running: `docker-compose ps`
- Check connection string in `appsettings.Development.json`

### Frontend Build Issues
- Clear node_modules: `rm -rf node_modules && pnpm install`
- Check Node.js version: `node --version` (should be 20+)

### CORS Issues
- CORS is configured for http://localhost:9000 in development
- Check `Program.cs` for CORS policy configuration