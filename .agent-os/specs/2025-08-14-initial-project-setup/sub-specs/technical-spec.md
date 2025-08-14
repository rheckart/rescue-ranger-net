# Technical Specification - Initial Project Setup

## Technical Requirements

### Backend Requirements

#### .NET Solution Structure
- **.NET Version**: 9.0
- **SDK**: Microsoft.NET.Sdk.Web
- **Language**: C# 13.0
- **Target Framework**: net9.0
- **Nullable Reference Types**: Enabled

#### Project Architecture
```
RescueRanger.sln
├── src/
│   ├── RescueRanger.Api/           # Web API project
│   ├── RescueRanger.Core/          # Domain models, interfaces
│   ├── RescueRanger.Infrastructure/# Data access, external services
│   └── RescueRanger.Contracts/     # Shared DTOs and contracts
└── tests/
    ├── RescueRanger.Api.Tests/     # API integration tests
    └── RescueRanger.Core.Tests/    # Unit tests
```

#### Required NuGet Packages

**RescueRanger.Api:**
- FastEndpoints (latest)
- FastEndpoints.Swagger (latest)
- Serilog.AspNetCore (latest)
- Serilog.Sinks.Console (latest)
- Serilog.Sinks.File (latest)

**RescueRanger.Core:**
- Ardalis.Result (latest)
- Vogen (latest)
- FluentValidation (latest)

**RescueRanger.Infrastructure:**
- Npgsql.EntityFrameworkCore.PostgreSQL (9.0.*)
- Microsoft.EntityFrameworkCore.Design (9.0.*)
- Microsoft.EntityFrameworkCore.Tools (9.0.*)

**Test Projects:**
- xunit (latest)
- xunit.runner.visualstudio (latest)
- Microsoft.NET.Test.Sdk (latest)
- FluentAssertions (latest)
- NSubstitute (latest)

### Frontend Requirements

#### Quasar/Vue.js Configuration
- **Node Version**: 22 LTS
- **Package Manager**: pnpm
- **Vue Version**: 3.5+
- **Quasar Version**: 2.17+
- **TypeScript**: 5.6+
- **Vite**: 5.4+

#### Project Structure
```
client/
├── src/
│   ├── assets/          # Static assets
│   ├── boot/            # Boot files (axios, etc.)
│   ├── components/      # Reusable components
│   ├── composables/     # Vue composables
│   ├── css/            # Global styles
│   ├── layouts/        # Layout components
│   ├── pages/          # Route pages
│   ├── router/         # Vue Router config
│   ├── services/       # API services
│   ├── stores/         # Pinia stores
│   └── types/          # TypeScript types
├── public/             # Public assets
├── .env.development    # Dev environment vars
├── quasar.config.ts    # Quasar configuration
├── tsconfig.json       # TypeScript config
└── package.json        # Dependencies
```

#### Required npm Packages
```json
{
  "dependencies": {
    "@quasar/extras": "latest",
    "quasar": "^2.17.0",
    "vue": "^3.5.0",
    "vue-router": "^4.4.0",
    "pinia": "^2.2.0",
    "axios": "^1.7.0",
    "@vueuse/core": "^11.0.0"
  },
  "devDependencies": {
    "@quasar/app-vite": "^2.0.0",
    "@types/node": "^22.0.0",
    "@typescript-eslint/eslint-plugin": "^8.0.0",
    "@typescript-eslint/parser": "^8.0.0",
    "eslint": "^9.0.0",
    "eslint-plugin-vue": "^9.0.0",
    "typescript": "~5.6.0",
    "vite": "^5.4.0",
    "vitest": "^2.0.0",
    "@vue/test-utils": "^2.4.0"
  }
}
```

### Development Environment

#### Docker Compose Configuration
```yaml
version: '3.9'
services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_USER: rescueranger
      POSTGRES_PASSWORD: development
      POSTGRES_DB: rescueranger_dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

#### JetBrains Rider Configuration

**Compound Configuration (.run/All.run.xml):**
```xml
<component name="ProjectRunConfigurationManager">
  <configuration name="All" type="CompoundRunConfigurationType">
    <toRun name="API" type="DotNetProject" />
    <toRun name="Frontend" type="js.build_tools.npm" />
  </configuration>
</component>
```

**API Run Configuration (.run/API.run.xml):**
```xml
<configuration name="API" type="DotNetProject">
  <option name="PROJECT_PATH" value="$PROJECT_DIR$/src/RescueRanger.Api/RescueRanger.Api.csproj" />
  <option name="LAUNCH_PROFILE" value="http" />
  <option name="WORKING_DIRECTORY" value="$PROJECT_DIR$/src/RescueRanger.Api" />
</configuration>
```

**Frontend Run Configuration (.run/Frontend.run.xml):**
```xml
<configuration name="Frontend" type="js.build_tools.npm">
  <package-json value="$PROJECT_DIR$/client/package.json" />
  <command value="run" />
  <scripts>
    <script value="dev" />
  </scripts>
  <node-interpreter value="project" />
  <envs />
</configuration>
```

### Configuration Files

#### Backend appsettings.Development.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=rescueranger_dev;Username=rescueranger;Password=development"
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": ["http://localhost:9000"]
  }
}
```

#### Frontend Proxy Configuration (quasar.config.ts)
```typescript
devServer: {
  port: 9000,
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
      changeOrigin: true
    }
  }
}
```

### API Specifications

#### Health Check Endpoint
- **Route**: GET /api/health
- **Response**: 200 OK
```json
{
  "status": "Healthy",
  "timestamp": "2025-08-14T10:00:00Z",
  "version": "1.0.0",
  "services": {
    "database": "Connected"
  }
}
```

#### Swagger Configuration
- **URL**: http://localhost:5000/swagger
- **OpenAPI Version**: 3.0.1
- **Documentation**: Auto-generated from FastEndpoints

### Development Ports

- **API**: http://localhost:5000
- **Frontend Dev Server**: http://localhost:9000
- **PostgreSQL**: localhost:5432
- **Swagger UI**: http://localhost:5000/swagger

### Environment Variables

#### Backend (.env)
```
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000
```

#### Frontend (.env.development)
```
VITE_API_URL=http://localhost:5000
VITE_APP_TITLE=Rescue Ranger
```

## Dependencies

### System Requirements
- .NET 9 SDK
- Node.js 22 LTS
- pnpm package manager
- Docker Desktop or Docker Engine
- PostgreSQL client tools (optional)
- JetBrains Rider 2024.3+

### External Services
- PostgreSQL 17 (via Docker)
- No external API dependencies for initial setup

### Development Tools
- Entity Framework Core CLI tools
- Quasar CLI
- Vue DevTools browser extension
- .NET diagnostics tools

## Performance Considerations

- **API Startup**: < 2 seconds
- **Frontend Dev Server**: < 5 seconds initial build
- **Hot Reload**: < 500ms for both backend and frontend
- **Database Connection**: Connection pooling configured
- **Development Build Size**: Not optimized (debugging enabled)

## Security Considerations

- **CORS**: Configured for local development only
- **HTTPS**: Disabled for local development
- **Database**: Default development credentials
- **Secrets**: Using development values, not production-ready
- **Authentication**: Not implemented in initial setup