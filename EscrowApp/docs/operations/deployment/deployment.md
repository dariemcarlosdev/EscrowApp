# 17 — Deployment

> Container-based deployment strategy for the EscrowApp.

## Overview

The EscrowApp uses a **Docker-based deployment** with a multi-stage build for minimal image size and a `docker-compose` configuration for local development that includes PostgreSQL.

## Deployment Artifacts

| File | Purpose |
|------|---------|
| `Dockerfile` | Multi-stage build: SDK (build+test) → ASP.NET runtime |
| `docker-compose.yml` | Local development: app + PostgreSQL |
| `.github/workflows/ci.yml` | CI pipeline: build, test, coverage |

## Dockerfile — Multi-Stage Build

```
Stage 1: Build (mcr.microsoft.com/dotnet/sdk:10.0-preview)
├── Restore dependencies (cached layer)
├── Build Release
├── Run tests
└── Publish to /app/publish

Stage 2: Runtime (mcr.microsoft.com/dotnet/aspnet:10.0-preview)
├── Copy published output
├── Run as non-root user (escrowapp)
├── Expose port 8080
└── ENTRYPOINT dotnet EscrowApp.dll
```

### Security Measures
- **Non-root user:** Application runs as `escrowapp` user, not root
- **Minimal image:** Runtime stage uses slim ASP.NET base (no SDK tools)
- **No secrets in image:** All configuration via environment variables

## Docker Compose — Local Development

```bash
# Start app + database
docker-compose up -d

# View logs
docker-compose logs -f app

# Stop
docker-compose down

# Stop and remove data
docker-compose down -v
```

### Services

| Service | Image | Port | Purpose |
|---------|-------|------|---------|
| `app` | Built from Dockerfile | 8080 | EscrowApp |
| `db` | postgres:17-alpine | 5432 | PostgreSQL database |

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Yes | PostgreSQL connection string |
| `Stripe__SecretKey` | Yes | Stripe API secret key |
| `Stripe__WebhookSecret` | Yes | Stripe webhook signing secret |
| `Authentication__ApiKey` | Yes | API authentication key |
| `POSTGRES_PASSWORD` | Yes | Database password |
| `ASPNETCORE_ENVIRONMENT` | No | Defaults to Production |

> ⚠️ **Never commit secrets to docker-compose.yml.** Use a `.env` file (git-ignored) or pass via command line.

### .env File (Example — DO NOT COMMIT)

```env
POSTGRES_PASSWORD=strong_random_password_here
STRIPE_SECRET_KEY=sk_test_your_key_here
STRIPE_WEBHOOK_SECRET=whsec_your_secret_here
API_KEY=your_api_key_here
```

## CI Pipeline — GitHub Actions

### Trigger Conditions
- Push to `main` or `develop` branches
- Pull requests targeting `main`

### Pipeline Steps
1. **Checkout** source code
2. **Setup .NET** 10.0
3. **Restore** NuGet dependencies
4. **Build** in Release mode with warnings-as-errors
5. **Test** with code coverage collection
6. **Upload** coverage report as artifact

### Future CI Enhancements
- [ ] Docker image build and push to container registry
- [ ] Database migration validation
- [ ] Security scanning (Snyk, Trivy)
- [ ] Deploy to staging on merge to `develop`
- [ ] Deploy to production on merge to `main`

## Production Deployment Targets (Future)

| Platform | Status | Notes |
|----------|--------|-------|
| Azure Container Apps | Planned | Preferred — serverless containers with Managed Identity |
| Azure App Service | Alternative | Simpler but less control |
| AWS ECS/Fargate | Alternative | If AWS is preferred |
| Self-hosted Docker | Fallback | docker-compose in VM |

### Production Checklist (Pre-Launch)

- [ ] HTTPS/TLS certificate configured
- [ ] Database connection string in Key Vault / Secrets Manager
- [ ] Stripe live keys (not test keys) in secure storage
- [ ] CORS configured for production domain
- [ ] Health check endpoint at `/health`
- [ ] Application Insights / monitoring configured
- [ ] Database migrations applied
- [ ] Backup strategy for PostgreSQL
- [ ] Rate limiting on API endpoints
- [ ] Error pages configured (not developer exception page)
