using RescueRanger.Api.Authorization;

namespace RescueRanger.Api.Features.Documentation;

/// <summary>
/// Endpoint for serving multi-tenant API documentation
/// </summary>
public sealed class ApiDocumentationEndpoint : Endpoint<ApiDocumentationRequest, ApiDocumentationResponse>
{
    private readonly ILogger<ApiDocumentationEndpoint> _logger;

    public ApiDocumentationEndpoint(ILogger<ApiDocumentationEndpoint> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/docs/api");
        Summary(s =>
        {
            s.Summary = "Get API documentation";
            s.Description = "Returns comprehensive documentation for the multi-tenant API including authentication flows and usage examples";
            s.Responses[200] = "API documentation";
        });
        
        AllowAnonymous();
    }

    public override async Task HandleAsync(ApiDocumentationRequest req, CancellationToken ct)
    {
        await Task.CompletedTask;

        var documentation = new ApiDocumentationResponse
        {
            Title = "RescueRanger Multi-Tenant API Documentation",
            Version = "1.0.0",
            LastUpdated = DateTime.UtcNow,
            Sections = new List<DocumentationSection>
            {
                ApiDocumentationHelpers.GetQuickStartSection(),
                ApiDocumentationHelpers.GetAuthenticationSection(),
                ApiDocumentationHelpers.GetTenantContextSection(),
                ApiDocumentationHelpers.GetEndpointCategoriesSection(),
                ApiDocumentationHelpers.GetErrorHandlingSection(),
                ApiDocumentationHelpers.GetHealthMonitoringSection(),
                ApiDocumentationHelpers.GetSecuritySection(),
                ApiDocumentationHelpers.GetTenantManagementSection(),
                ApiDocumentationHelpers.GetBestPracticesSection(),
                ApiDocumentationHelpers.GetTroubleshootingSection()
            }
        };

        Response = documentation;

        _logger.LogDebug("API documentation served for request type {RequestType}", req.Format);
    }
}

/// <summary>
/// Request model for API documentation
/// </summary>
public sealed class ApiDocumentationRequest
{
    /// <summary>
    /// Format of documentation to return (json, markdown)
    /// </summary>
    public string Format { get; set; } = "json";

    /// <summary>
    /// Specific sections to include (empty for all)
    /// </summary>
    public List<string> Sections { get; set; } = new();
}

/// <summary>
/// Response model for API documentation
/// </summary>
public sealed class ApiDocumentationResponse
{
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public List<DocumentationSection> Sections { get; set; } = new();
}

/// <summary>
/// Documentation section
/// </summary>
public sealed class DocumentationSection
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<CodeExample> Examples { get; set; } = new();
    public List<DocumentationSection> SubSections { get; set; } = new();
}

/// <summary>
/// Code example for documentation
/// </summary>
public sealed class CodeExample
{
    public string Title { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public static partial class ApiDocumentationHelpers
{
    public static DocumentationSection GetQuickStartSection()
    {
        return new DocumentationSection
        {
            Id = "quickstart",
            Title = "Quick Start Guide",
            Content = @"
# Quick Start Guide

Get started with the RescueRanger Multi-Tenant API in minutes.

## 1. Obtain Access Credentials

Contact your system administrator to get:
- JWT authentication token
- Your tenant subdomain
- API base URL

## 2. Set Up Your Environment

Configure your API client with:
- Base URL: `https://{your-tenant}.rescueranger.com/api`
- Authentication: Bearer token
- Content-Type: application/json

## 3. Make Your First Request

Test connectivity with a simple health check.
",
            Examples = new List<CodeExample>
            {
                new CodeExample
                {
                    Title = "Health Check Request",
                    Language = "bash",
                    Code = @"curl -X GET \
  'https://happytails.rescueranger.com/api/health' \
  -H 'Authorization: Bearer YOUR_JWT_TOKEN'",
                    Description = "Check if the API is responding for your tenant"
                },
                new CodeExample
                {
                    Title = "JavaScript/Node.js Setup",
                    Language = "javascript",
                    Code = @"const axios = require('axios');

const api = axios.create({
  baseURL: 'https://happytails.rescueranger.com/api',
  headers: {
    'Authorization': 'Bearer YOUR_JWT_TOKEN',
    'Content-Type': 'application/json'
  }
});

// Test connection
api.get('/health')
  .then(response => console.log('API connected:', response.data))
  .catch(error => console.error('Connection failed:', error));",
                    Description = "Set up an API client in Node.js"
                }
            }
        };
    }

    public static DocumentationSection GetAuthenticationSection()
    {
        return new DocumentationSection
        {
            Id = "authentication",
            Title = "Authentication & Authorization",
            Content = @"
# Authentication & Authorization

The API uses JWT-based authentication with tenant-scoped authorization.

## JWT Token Structure

Your JWT token contains:
- User identity information
- Tenant membership
- Role assignments
- Expiration time

## Roles & Permissions

### Tenant Roles
- **Admin**: Full tenant management
- **Manager**: User and data management
- **User**: Read and limited write access
- **Viewer**: Read-only access

### System Roles
- **System Admin**: Cross-tenant access and system management

## Token Lifecycle

1. **Obtain Token**: Login with credentials
2. **Use Token**: Include in Authorization header
3. **Refresh Token**: Before expiration
4. **Handle Expiry**: Re-authenticate when needed
",
            Examples = new List<CodeExample>
            {
                new CodeExample
                {
                    Title = "Login Request",
                    Language = "bash",
                    Code = @"curl -X POST \
  'https://happytails.rescueranger.com/api/auth/login' \
  -H 'Content-Type: application/json' \
  -d '{
    ""email"": ""user@happytails.org"",
    ""password"": ""your_password""
  }'",
                    Description = "Authenticate and obtain a JWT token"
                },
                new CodeExample
                {
                    Title = "Authenticated Request",
                    Language = "bash",
                    Code = @"curl -X GET \
  'https://happytails.rescueranger.com/api/horses' \
  -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIs...'",
                    Description = "Include JWT token in requests"
                }
            }
        };
    }

    public static DocumentationSection GetTenantContextSection()
    {
        return new DocumentationSection
        {
            Id = "tenant-context",
            Title = "Tenant Context & Multi-Tenancy",
            Content = @"
# Tenant Context & Multi-Tenancy

The API automatically resolves your tenant context from the subdomain.

## Tenant Resolution

- **URL Format**: `https://{tenant-subdomain}.rescueranger.com/api/`
- **Automatic**: No manual tenant selection required
- **Scoped**: All operations are tenant-scoped automatically

## Data Isolation

- **Complete Separation**: Each tenant's data is completely isolated
- **No Cross-Tenant Access**: Users can only access their tenant's data
- **Automatic Filtering**: Database queries are automatically filtered by tenant

## Tenant Switching

System administrators can switch tenant context using headers.
",
            Examples = new List<CodeExample>
            {
                new CodeExample
                {
                    Title = "Tenant-Specific Request",
                    Language = "bash",
                    Code = @"# Request to Happy Tails rescue organization
curl -X GET \
  'https://happytails.rescueranger.com/api/horses' \
  -H 'Authorization: Bearer YOUR_TOKEN'

# Request to Second Chance rescue organization  
curl -X GET \
  'https://secondchance.rescueranger.com/api/horses' \
  -H 'Authorization: Bearer YOUR_TOKEN'",
                    Description = "Different subdomains access different tenant data"
                },
                new CodeExample
                {
                    Title = "System Admin Tenant Override",
                    Language = "bash",
                    Code = @"curl -X GET \
  'https://admin.rescueranger.com/api/admin/tenants' \
  -H 'Authorization: Bearer SYSTEM_ADMIN_TOKEN' \
  -H 'X-Override-Tenant: 12345678-1234-1234-1234-123456789012'",
                    Description = "System admins can override tenant context"
                }
            }
        };
    }

    public static DocumentationSection GetEndpointCategoriesSection()
    {
        return new DocumentationSection
        {
            Id = "endpoint-categories",
            Title = "API Endpoint Categories",
            Content = @"
# API Endpoint Categories

The API is organized into logical categories for different types of operations.
",
            SubSections = new List<DocumentationSection>
            {
                new DocumentationSection
                {
                    Id = "horses",
                    Title = "Horse Management",
                    Content = @"
## Horse Management Endpoints

Manage horse records, medical information, and status updates.

- `GET /horses` - List all horses
- `POST /horses` - Create a new horse record
- `GET /horses/{id}` - Get horse details
- `PUT /horses/{id}` - Update horse information
- `DELETE /horses/{id}` - Archive horse record
",
                    Examples = new List<CodeExample>
                    {
                        new CodeExample
                        {
                            Title = "Create Horse Record",
                            Language = "bash",
                            Code = @"curl -X POST \
  'https://happytails.rescueranger.com/api/horses' \
  -H 'Authorization: Bearer YOUR_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{
    ""name"": ""Thunder"",
    ""breed"": ""Quarter Horse"",
    ""age"": 8,
    ""status"": ""Available"",
    ""medicalNotes"": ""Healthy, up to date on vaccinations""
  }'",
                            Description = "Create a new horse record in your tenant"
                        }
                    }
                }
            }
        };
    }

    public static DocumentationSection GetErrorHandlingSection()
    {
        return new DocumentationSection
        {
            Id = "error-handling",
            Title = "Error Handling",
            Content = @"
# Error Handling

The API uses RFC 7807 Problem Details format for consistent error responses.

## Common Error Types

### Tenant-Related Errors
- **404 Tenant Not Found**: Invalid or missing tenant context
- **403 Cross-Tenant Access**: Attempting to access another tenant's data

### Authentication Errors
- **401 Unauthorized**: Missing or invalid JWT token
- **403 Forbidden**: Insufficient permissions

### Validation Errors
- **400 Bad Request**: Invalid request data
- **422 Unprocessable Entity**: Validation failures
",
            Examples = new List<CodeExample>
            {
                new CodeExample
                {
                    Title = "Cross-Tenant Access Error",
                    Language = "json",
                    Code = @"{
  ""type"": ""https://tools.ietf.org/html/rfc7231#section-6.5.3"",
  ""title"": ""Forbidden"",
  ""status"": 403,
  ""detail"": ""Access denied: User does not belong to this tenant"",
  ""instance"": ""/api/horses/123""
}",
                    Description = "Error response when attempting cross-tenant access"
                }
            }
        };
    }

    public static DocumentationSection GetHealthMonitoringSection()
    {
        return new DocumentationSection
        {
            Id = "health-monitoring",
            Title = "Health Monitoring",
            Content = @"
# Health Monitoring

Monitor API and tenant-specific health with dedicated endpoints.

## Health Check Endpoints

- `GET /health` - Overall API health
- `GET /health/tenant` - Tenant-specific health checks
- `GET /health/tenant/metrics` - Tenant performance metrics (admin only)

## Health Status Values

- **Healthy**: All systems operational
- **Degraded**: Minor issues, still functional
- **Unhealthy**: Significant problems detected
",
            Examples = new List<CodeExample>
            {
                new CodeExample
                {
                    Title = "Tenant Health Check",
                    Language = "bash",
                    Code = @"curl -X GET \
  'https://happytails.rescueranger.com/api/health/tenant' \
  -H 'Authorization: Bearer YOUR_TOKEN'",
                    Description = "Check tenant-specific health status"
                }
            }
        };
    }

    public static DocumentationSection GetSecuritySection()
    {
        return new DocumentationSection
        {
            Id = "security",
            Title = "Security Features",
            Content = @"
# Security Features

The API includes comprehensive security measures for multi-tenant operations.

## Automatic Security Features

- **Tenant Isolation**: Complete data separation between tenants
- **Access Control**: Role-based permissions within tenants
- **Audit Logging**: All access and operations are logged
- **Cross-Tenant Protection**: Automatic prevention of cross-tenant access

## Best Practices

1. **Token Security**: Never expose JWT tokens in URLs or logs
2. **HTTPS Only**: Always use HTTPS in production
3. **Token Rotation**: Regularly refresh authentication tokens
4. **Principle of Least Privilege**: Use the minimum required permissions
"
        };
    }

    public static DocumentationSection GetTenantManagementSection()
    {
        return new DocumentationSection
        {
            Id = "tenant-management",
            Title = "Tenant Management (System Admin)",
            Content = @"
# Tenant Management

System administrators can manage tenants using dedicated endpoints.

## Tenant Management Endpoints

- `GET /admin/tenants` - List all tenants
- `POST /admin/tenants` - Create new tenant
- `GET /admin/tenants/{id}` - Get tenant details
- `PUT /admin/tenants/{id}` - Update tenant
- `POST /admin/tenants/{id}/suspend` - Suspend tenant

## User Management

- `GET /admin/users` - List users across tenants
- `POST /admin/users/invite` - Invite user to tenant
- `PUT /admin/users/{id}/role` - Update user role
",
            Examples = new List<CodeExample>
            {
                new CodeExample
                {
                    Title = "Create New Tenant",
                    Language = "bash",
                    Code = @"curl -X POST \
  'https://admin.rescueranger.com/api/admin/tenants' \
  -H 'Authorization: Bearer SYSTEM_ADMIN_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{
    ""name"": ""New Rescue Organization"",
    ""subdomain"": ""newrescue"",
    ""contactEmail"": ""admin@newrescue.org"",
    ""status"": ""Active""
  }'",
                    Description = "Create a new tenant (system admin only)"
                }
            }
        };
    }

    public static DocumentationSection GetBestPracticesSection()
    {
        return new DocumentationSection
        {
            Id = "best-practices",
            Title = "Best Practices",
            Content = @"
# Best Practices

Follow these guidelines for optimal API usage and security.

## General Guidelines

1. **Use Appropriate HTTP Methods**: GET for retrieval, POST for creation, etc.
2. **Handle Errors Gracefully**: Always check response status codes
3. **Implement Retry Logic**: For transient failures
4. **Cache Responses**: When appropriate to reduce API calls

## Security Best Practices

1. **Secure Token Storage**: Store JWT tokens securely
2. **Validate SSL Certificates**: Always verify HTTPS certificates
3. **Monitor Usage**: Track API usage patterns
4. **Report Issues**: Contact support for security concerns

## Performance Optimization

1. **Use Pagination**: For large result sets
2. **Request Only Needed Fields**: Use field selection when available
3. **Batch Operations**: Group related operations together
4. **Monitor Rate Limits**: Respect API rate limiting
"
        };
    }

    public static DocumentationSection GetTroubleshootingSection()
    {
        return new DocumentationSection
        {
            Id = "troubleshooting",
            Title = "Troubleshooting",
            Content = @"
# Troubleshooting

Common issues and their solutions.

## Authentication Issues

**Problem**: 401 Unauthorized responses
**Solution**: Check JWT token validity and format

**Problem**: 403 Forbidden responses  
**Solution**: Verify user has required permissions for the operation

## Tenant Issues

**Problem**: 404 Tenant Not Found
**Solution**: Verify subdomain is correct and tenant is active

**Problem**: Cross-tenant access errors
**Solution**: Ensure you're accessing the correct tenant subdomain

## Performance Issues

**Problem**: Slow response times
**Solution**: Check tenant health metrics and contact support if needed

**Problem**: Rate limiting errors
**Solution**: Implement exponential backoff and reduce request frequency

## Getting Help

- Check the health endpoints for system status
- Review audit logs for security events
- Contact support with specific error messages
- Include request IDs when reporting issues
"
        };
    }
}