# Implementation Tasks - Initial Project Setup

## Task 1: Initialize Backend Solution Structure

### 1.1 Create Solution and Projects
- [ ] Run `dotnet new sln -n RescueRanger` in root directory
- [ ] Create src and tests directories
- [ ] Run `dotnet new webapi -n RescueRanger.Api -o src/RescueRanger.Api`
- [ ] Run `dotnet new classlib -n RescueRanger.Core -o src/RescueRanger.Core`
- [ ] Run `dotnet new classlib -n RescueRanger.Infrastructure -o src/RescueRanger.Infrastructure`
- [ ] Run `dotnet new classlib -n RescueRanger.Contracts -o src/RescueRanger.Contracts`
- [ ] Add all projects to solution with `dotnet sln add` commands

### 1.2 Configure Project References
- [ ] Add Core reference to Infrastructure project
- [ ] Add Core and Infrastructure references to Api project
- [ ] Add Contracts reference to Api and Core projects
- [ ] Set C# language version to 13.0 and enable nullable reference types

### 1.3 Install NuGet Packages
- [ ] Install FastEndpoints and FastEndpoints.Swagger in Api project
- [ ] Install Serilog packages in Api project
- [ ] Install Entity Framework Core packages in Infrastructure project
- [ ] Install Ardalis.Result and Vogen in Core project
- [ ] Install test packages in test projects

### 1.4 Setup Program.cs with FastEndpoints
- [ ] Configure Serilog logging
- [ ] Setup FastEndpoints with service registration
- [ ] Configure Swagger documentation
- [ ] Add CORS policy for development
- [ ] Configure Entity Framework with PostgreSQL connection

## Task 2: Create Frontend Quasar Project

### 2.1 Initialize Quasar Project
- [ ] Run `pnpm create quasar` in root directory, output to `client` folder
- [ ] Select App with Vite, TypeScript, Quasar v2 (Vue 3)
- [ ] Configure with Vue Router, Pinia, ESLint, and Axios
- [ ] Install additional dependencies (@vueuse/core, etc.)

### 2.2 Configure TypeScript and Build Settings
- [ ] Update tsconfig.json with strict type checking
- [ ] Configure path aliases for clean imports
- [ ] Setup environment variables in .env.development
- [ ] Configure Vite proxy for API calls in quasar.config.ts

### 2.3 Create Base Project Structure
- [ ] Create services directory with api.ts base configuration
- [ ] Create types directory for TypeScript interfaces
- [ ] Create composables directory for reusable logic
- [ ] Setup base layout with Quasar components

### 2.4 Implement Health Check Service
- [ ] Create health.service.ts with API client
- [ ] Create HealthCheck.vue component
- [ ] Add route for health check page
- [ ] Verify API communication works

## Task 3: Setup Development Environment

### 3.1 Create Docker Compose Configuration
- [ ] Create docker-compose.yml with PostgreSQL service
- [ ] Configure volumes for data persistence
- [ ] Set development credentials
- [ ] Test database connection with `docker-compose up`

### 3.2 Configure Application Settings
- [ ] Create appsettings.Development.json with connection string
- [ ] Setup CORS configuration for localhost:9000
- [ ] Configure Serilog for console and file output
- [ ] Add health check configuration

### 3.3 Setup JetBrains Rider Configurations
- [ ] Create .run directory for run configurations
- [ ] Create API.run.xml for backend project
- [ ] Create Frontend.run.xml for npm dev script
- [ ] Create All.run.xml compound configuration
- [ ] Test running both projects simultaneously

### 3.4 Initialize Git Repository
- [ ] Run `git init` in project root
- [ ] Create comprehensive .gitignore file
- [ ] Add .env files to gitignore
- [ ] Create initial commit with base structure

## Task 4: Implement Health Check API

### 4.1 Create Health Check Endpoints
- [ ] Create HealthCheckEndpoint.cs with FastEndpoints
- [ ] Implement basic /health endpoint
- [ ] Implement detailed /health/ready endpoint with database check
- [ ] Create response DTOs in Contracts project

### 4.2 Configure Health Checks
- [ ] Register health check services in Program.cs
- [ ] Add database health check
- [ ] Configure health check UI endpoint (optional)
- [ ] Test health endpoints with curl/Postman

### 4.3 Create API Info Endpoint
- [ ] Create ApiInfoEndpoint.cs
- [ ] Return API metadata and version information
- [ ] Document in Swagger
- [ ] Test endpoint functionality

## Task 5: Verify Integration and Documentation

### 5.1 Test Full Stack Communication
- [ ] Start Docker Compose for PostgreSQL
- [ ] Run API project and verify it starts
- [ ] Run frontend dev server
- [ ] Verify frontend can call health check endpoint
- [ ] Check CORS is working correctly

### 5.2 Verify Development Experience
- [ ] Test hot reload for C# code changes
- [ ] Test hot reload for Vue components
- [ ] Verify debugging works in Rider for both projects
- [ ] Ensure Swagger documentation is generated

### 5.3 Create Project Documentation
- [ ] Update README.md with setup instructions
- [ ] Document required system dependencies
- [ ] Add troubleshooting section
- [ ] Include Rider configuration steps

### 5.4 Run Initial Tests
- [ ] Create basic API integration test for health endpoint
- [ ] Create basic Vue component test
- [ ] Ensure all projects build without errors
- [ ] Run `dotnet test` to verify test setup

## Completion Checklist

Before marking complete, verify:
- [ ] Solution opens in JetBrains Rider without errors
- [ ] Compound run configuration starts both API and frontend
- [ ] API responds at http://localhost:5000/health
- [ ] Frontend loads at http://localhost:9000
- [ ] Frontend successfully calls API health endpoint
- [ ] Swagger UI available at http://localhost:5000/swagger
- [ ] PostgreSQL connection verified through health check
- [ ] All projects follow Clean Architecture principles
- [ ] TypeScript strict mode enabled and no errors
- [ ] Hot reload works for both backend and frontend