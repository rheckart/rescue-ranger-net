# Initial Project Setup Specification

## Overview

Set up the foundational .NET 9 backend solution and Vue.js/Quasar frontend project with proper structure, configuration, and JetBrains Rider integration to enable simultaneous development and debugging of both the API and client applications.

## User Stories

### Story 1: Developer Environment Setup
**As a** developer
**I want to** open the project in JetBrains Rider and run both backend and frontend simultaneously
**So that I can** efficiently develop and debug the full-stack application

**Workflow:**
1. Open the solution file in JetBrains Rider
2. Configure compound run configuration for backend and frontend
3. Press Run to start both API (port 5000) and frontend dev server (port 9000)
4. Navigate to localhost:9000 to see the Quasar app consuming the API
5. Set breakpoints in both C# and TypeScript code for debugging

### Story 2: Project Structure Navigation
**As a** developer
**I want to** have a well-organized project structure
**So that I can** quickly locate and work on different parts of the application

**Workflow:**
1. Navigate to `/src/RescueRanger.Api` for backend API code
2. Navigate to `/src/RescueRanger.Core` for domain models and business logic
3. Navigate to `/src/RescueRanger.Infrastructure` for data access and external services
4. Navigate to `/client` for all frontend Vue.js/Quasar code
5. Find configuration files at the root level for easy access

## Spec Scope

1. **Backend Solution Structure**: Create .NET 9 solution with Clean Architecture projects (Api, Core, Infrastructure, Tests) configured with FastEndpoints, Entity Framework Core, and PostgreSQL connection
2. **Frontend Project Setup**: Initialize Quasar.dev project with TypeScript, Pinia state management, Vue Router, and development proxy configuration for API calls
3. **Development Configuration**: Set up Docker Compose for local PostgreSQL, environment configurations, and JetBrains Rider compound run configurations
4. **Initial API Endpoints**: Create health check endpoint and basic API versioning setup with Swagger documentation
5. **Frontend Foundation**: Create base layout with Quasar components, routing structure, and API client service with axios configuration

## Out of Scope

- User authentication implementation (JWT, OAuth, magic links)
- Database migrations and entity models
- Business logic implementation
- UI/UX design beyond basic Quasar layout
- Production deployment configuration
- CI/CD pipeline setup
- Multi-tenancy implementation

## Expected Deliverable

1. **Functional Development Environment**: Both backend API and frontend dev server start successfully from Rider with a single run configuration, API responds at http://localhost:5000, frontend at http://localhost:9000
2. **Verified API Communication**: Frontend successfully calls the health check endpoint and displays the response, confirming proxy configuration and CORS settings are correct
3. **Project Structure Validation**: All projects build without errors, tests run successfully, and the solution follows Clean Architecture principles with proper project references