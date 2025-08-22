# Technical Stack

## Core Technologies

- **Application Framework:** Microsoft .NET 9
- **Programming Language:** C# 13
- **Database System:** PostgreSQL 17+
- **ORM:** Entity Framework Core
- **Ardalis.Result:** Result pattern for API responses
- **Vogen:** Strongly-typed primary keys
- **API Architecture:** FastEndpoints Nuget Package together with ASP.NET Core Web Minimal API

## Frontend Technologies

- **JavaScript Framework:** Vue.js 3 (latest stable)
- **Build Tool:** Vite
- **Import Strategy:** Node.js modules
- **Package Manager:** pnpm
- **Node Version:** 22 LTS

## UI/UX Stack

- **CSS Framework:** Quasar.dev CSS styles / Quasar Sass/SCSS variables
- **UI Component Library:** Quasar.dev
- **UI Installation:** Via Quasar CLI tool
- **Fonts Provider:** Google Fonts (self-hosted for performance)
- **Icon Library:** Quasar.dev icon components
- **Mobile Framework:** Progressive Web App (PWA) with offline support

## Infrastructure

- **Application Hosting:** Digital Ocean App Platform
- **Database Hosting:** Digital Ocean Managed PostgreSQL
- **Database Backups:** Daily automated with point-in-time recovery
- **Asset Hosting:** Amazon S3
- **CDN:** CloudFront
- **Asset Access:** Private with signed URLs for sensitive documents

## Development & Deployment

- **Version Control:** Git with GitHub
- **CI/CD Platform:** GitHub Actions
- **Deployment Solution:** Automated deployment via GitHub Actions
- **Environment Strategy:**
  - Production: main branch → Digital Ocean App Platform
  - Staging: staging branch → Digital Ocean staging environment
  - Development: Local Docker containers
- **Testing Framework:** xUnit for .NET, Vitest for Vue.js
- **Code Repository URL:** https://github.com/rheckart/rescue-ranger-net

## Security & Authentication

- **Authentication:** ASP.NET Core Identity with JWT tokens
- **SSO Integration:** Google and Facebook OAuth
- **Magic Links:** Email-based passwordless authentication
- **Multi-tenancy:** Schema-based isolation in PostgreSQL
- **CAPTCHA:** Cloudflare Turnstile for signup protection

## Additional Services

- **Email Service:** SendGrid for transactional emails
- **Push Notifications:** Firebase Cloud Messaging (FCM)
- **File Storage:** S3-compatible object storage
- **Monitoring:** Application Insights for .NET, Sentry for frontend
- **Analytics:** Privacy-focused analytics with Plausible

## Development Standards

- **API Documentation:** OpenAPI/Swagger
- **Code Style:** .NET conventions with EditorConfig
- **Database Migrations:** EF Core Code-First migrations
- **Environment Configuration:** appsettings.json with environment overrides
- **Secret Management:** Azure Key Vault for production, User Secrets for development
- **Primary Keys** Utilize the Vogen Nuget package for strongly-typed primary key creation
- **Exceptions** Utilize the Ardalis.Result Nuget package to return results from methods. Use exceptions only when necessary