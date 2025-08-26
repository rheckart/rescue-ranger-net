# Tech Stack

## Context

Global tech stack defaults for Agent OS projects, overridable in project-specific `.agent-os/product/tech-stack.md`.

- App Framework: Microsoft .NET 9
- Language: C# 13
- Primary Database: PostgreSQL 17+
- ORM: Microsoft Entity Framework Core
- JavaScript Framework: VueJS latest stable
- Build Tool: Vite
- Import Strategy: Node.js modules
- Package Manager: pnpm
- Node Version: 22 LTS
- CSS Framework: TailwindCSS 4.0+
- UI Components: Quasar.dev
- UI Installation: Via Quasar.dev CLI tool
- Font Provider: Google Fonts
- Font Loading: Self-hosted for performance
- Icons: Quasar.dev components
- Application Hosting: Digital Ocean App Platform/Droplets
- Hosting Region: Primary region based on user base
- Database Hosting: Digital Ocean Managed PostgreSQL
- Database Backups: Daily automated
- Asset Storage: Amazon S3
- CDN: CloudFront
- Asset Access: Private with signed URLs
- CI/CD Platform: GitHub Actions
- CI/CD Trigger: Push to main/staging branches
- Tests: Run before deployment
- Production Environment: main branch
- Staging Environment: staging branch
