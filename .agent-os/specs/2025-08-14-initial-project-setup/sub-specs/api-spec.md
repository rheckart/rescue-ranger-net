# API Specification - Initial Project Setup

## API Overview

Initial API setup with FastEndpoints framework, health checks, versioning, and Swagger documentation. No business logic endpoints are implemented in this phase.

## Base Configuration

### API Versioning Strategy
- **Version Header**: `X-API-Version`
- **Default Version**: `1.0`
- **Version in URL**: `/api/v1/` (optional, header preferred)

### Global Route Prefix
- All API endpoints prefixed with `/api`
- Health endpoints at root level

## Endpoints

### 1. Health Check Endpoint

#### Basic Health Check
**Endpoint**: `GET /health`
**Description**: Simple health check for load balancers
**Authentication**: None
**Response**: 200 OK
```json
{
  "status": "Healthy"
}
```

#### Detailed Health Check
**Endpoint**: `GET /health/ready`
**Description**: Comprehensive health check with service statuses
**Authentication**: None
**Response**: 200 OK
```json
{
  "status": "Healthy",
  "timestamp": "2025-08-14T10:00:00Z",
  "version": "1.0.0",
  "environment": "Development",
  "services": {
    "database": {
      "status": "Healthy",
      "responseTime": "15ms"
    }
  },
  "system": {
    "uptime": "00:05:23",
    "memoryUsage": "125 MB"
  }
}
```

**Response**: 503 Service Unavailable (if any service is unhealthy)
```json
{
  "status": "Unhealthy",
  "timestamp": "2025-08-14T10:00:00Z",
  "version": "1.0.0",
  "environment": "Development",
  "services": {
    "database": {
      "status": "Unhealthy",
      "error": "Connection timeout"
    }
  }
}
```

### 2. API Information Endpoint

**Endpoint**: `GET /api/info`
**Description**: Returns API metadata and capabilities
**Authentication**: None
**Response**: 200 OK
```json
{
  "name": "Rescue Ranger API",
  "version": "1.0.0",
  "description": "Horse rescue management system API",
  "documentation": "http://localhost:5000/swagger",
  "supportedVersions": ["1.0"],
  "features": {
    "authentication": false,
    "multiTenancy": false,
    "offlineSync": false
  }
}
```

## FastEndpoints Configuration

### Endpoint Base Class
```csharp
public abstract class EndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    protected override void Configure()
    {
        // Default configuration for all endpoints
        Options(x => x
            .WithTags("General")
            .ProducesProblemDetails()
        );
    }
}
```

### Request/Response Models

#### Health Check Response
```csharp
public class HealthCheckResponse
{
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public Dictionary<string, ServiceStatus> Services { get; set; } = new();
    public SystemInfo? System { get; set; }
}

public class ServiceStatus
{
    public string Status { get; set; } = "Unknown";
    public string? ResponseTime { get; set; }
    public string? Error { get; set; }
}

public class SystemInfo
{
    public string Uptime { get; set; } = "00:00:00";
    public string MemoryUsage { get; set; } = "0 MB";
}
```

## Swagger/OpenAPI Configuration

### Swagger Settings
```csharp
app.UseSwaggerGen(settings =>
{
    settings.Title = "Rescue Ranger API";
    settings.Version = "v1.0";
    settings.Description = "API for managing horse rescue operations";
    settings.ContactName = "Rescue Ranger Team";
    settings.ContactEmail = "support@rescueranger.app";
    settings.LicenseName = "MIT";
});
```

### Swagger UI Access
- **URL**: http://localhost:5000/swagger
- **JSON Schema**: http://localhost:5000/swagger/v1/swagger.json

## CORS Configuration

### Development CORS Policy
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:9000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

## Error Handling

### Global Exception Handler
```csharp
app.UseExceptionHandler("/error");
app.Map("/error", () => Results.Problem());
```

### Problem Details Response
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Validation errors occurred",
  "instance": "/api/endpoint",
  "traceId": "00-abc123-00",
  "errors": {
    "field": ["Error message"]
  }
}
```

## Request/Response Logging

### Serilog Configuration
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File("logs/api-.json", 
        rollingInterval: RollingInterval.Day,
        formatter: new JsonFormatter())
    .CreateLogger();
```

### Request Logging Middleware
```csharp
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
    };
});
```

## Rate Limiting (Future)

### Planned Configuration
```csharp
// Not implemented in initial setup
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

## API Client Configuration (Frontend)

### Axios Setup
```typescript
// services/api.ts
import axios, { AxiosInstance } from 'axios';

const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
    'X-API-Version': '1.0'
  }
});

// Request interceptor
apiClient.interceptors.request.use(
  (config) => {
    // Future: Add auth token
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Handle common errors
    if (error.response?.status === 503) {
      console.error('Service unavailable');
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

### Health Check Service
```typescript
// services/health.service.ts
import apiClient from './api';

export interface HealthCheckResponse {
  status: string;
  timestamp: string;
  version: string;
  services: Record<string, any>;
}

export const healthService = {
  async checkHealth(): Promise<HealthCheckResponse> {
    const response = await apiClient.get<HealthCheckResponse>('/health/ready');
    return response.data;
  },
  
  async getApiInfo(): Promise<any> {
    const response = await apiClient.get('/api/info');
    return response.data;
  }
};
```

## Testing Strategy

### API Integration Tests
```csharp
[TestClass]
public class HealthCheckTests : TestBase
{
    [Test]
    public async Task Health_Check_Returns_Healthy()
    {
        // Arrange
        var client = CreateClient();
        
        // Act
        var response = await client.GetAsync("/health");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
```

## Performance Requirements

- **Response Time**: < 100ms for health checks
- **Throughput**: Support 100 concurrent requests
- **Startup Time**: < 2 seconds
- **Memory Usage**: < 200MB for API process

## Security Headers

### Development Headers
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});
```