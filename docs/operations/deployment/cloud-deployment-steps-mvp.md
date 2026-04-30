# Cloud Deployment Steps — #8

> Merge point: Requires #3 (auth) + #6 (secrets) + optional #7 (webhooks)
> 
> Status: **Planned for #8** — Prepare for production deployment after auth and secrets are verified.

## Overview

The EscrowApp is containerized and ready for cloud deployment. This doc covers the **pre-launch checklist, deployment steps, and post-launch verification** for Azure Container Apps (preferred) or alternative platforms.

## Pre-Launch Checklist (Critical Path)

| Check | Status | Owner | When |
|-------|--------|-------|------|
| **Fintech attorney review** | 🔴 BLOCKED | Legal | Before launch (8–12 weeks) |
| **Money transmitter license assessment** | 🔴 BLOCKED | Legal | Before launch |
| **Terms of Service review** | 🔴 BLOCKED | Legal | Before launch |
| **OWASP security audit** | ✅ DONE | Security | 2026-04-11 |
| **Secrets rotation test** | 🟡 PENDING | DevOps | Week of #6 |
| **Database backup test** | 🟡 PENDING | DevOps | Week of #8 |
| **Load testing** | 🟡 PENDING | QA | Pre-launch (1–2 weeks before go-live) |
| **Health check endpoint** | 🔴 BLOCKED | Dev | Before #8 |
| **Monitoring setup** | 🔴 BLOCKED | DevOps | Before #8 |
| **Runbook creation** | 🔴 BLOCKED | DevOps | Before #8 |

## Prerequisites

Before attempting deployment:

1. ✅ **#3 ASP.NET Identity** — Users can log in
2. ✅ **#6 Production secrets** — No hardcoded keys
3. ✅ **All tests passing** — 51+ tests, 0 failures
4. 🔴 **Legal clearance** — Fintech attorney approval (NOT MVP GATE, but pre-launch)
5. 🔴 **Domain registered** — e.g., `nextruzt.io`
6. 🔴 **SSL/TLS certificate** — Stripe requires HTTPS
7. 🔴 **Stripe live keys** (not test keys)

## Deployment Architecture

```
GitHub
  ↓
CI Pipeline (.github/workflows/ci.yml)
  ├── Checkout
  ├── Build + Test
  └── (Future) Push Docker image to registry
        ↓
Azure Container Registry (ACR)
  ↓
Azure Container Apps
  ├── App container (port 8080)
  ├── PostgreSQL server (managed)
  └── Key Vault (secrets)
        ↓
Azure Front Door (CDN + WAF)
  ├── HTTPS (TLS 1.2+)
  ├── CORS (nextruzt.io only)
  └── Rate limiting
```

## Deployment Steps — Azure Container Apps (Recommended)

### Step 1: Prepare Azure Subscription

```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "subscription-id"

# Create resource group
az group create \
  --name escrow-app-rg \
  --location eastus

# Create container registry (private)
az acr create \
  --resource-group escrow-app-rg \
  --name escrowapp \
  --sku Standard
```

### Step 2: Build & Push Docker Image

```bash
# Build locally (or push from GitHub Actions)
docker build -t escrowapp:1.0.0 .

# Tag for ACR
docker tag escrowapp:1.0.0 escrowapp.azurecr.io/escrowapp:1.0.0

# Login to ACR
az acr login --name escrowapp

# Push image
docker push escrowapp.azurecr.io/escrowapp:1.0.0

# Verify
az acr repository list --name escrowapp
```

### Step 3: Create PostgreSQL Database

```bash
# Create Azure Database for PostgreSQL (Flexible Server)
az postgres flexible-server create \
  --resource-group escrow-app-rg \
  --name escrow-db \
  --location eastus \
  --admin-user dbadmin \
  --admin-password $(openssl rand -base64 32) \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 17 \
  --high-availability Disabled \
  --public-access Enabled

# Save connection string to secure location (Key Vault)
# Format: "Host=escrow-db.postgres.database.azure.com;Username=dbadmin;Password=...;Database=escrowapp"
```

### Step 4: Create Azure Key Vault

```bash
# Create Key Vault
az keyvault create \
  --resource-group escrow-app-rg \
  --name escrow-vault \
  --location eastus

# Add secrets (retrieve from 1Password or secure source)
az keyvault secret set --vault-name escrow-vault \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Host=escrow-db.postgres.database.azure.com;Username=dbadmin;Password=...;Database=escrowapp"

az keyvault secret set --vault-name escrow-vault \
  --name "Stripe--SecretKey" \
  --value "sk_live_..." # ← Stripe live key (never test key)

az keyvault secret set --vault-name escrow-vault \
  --name "Stripe--WebhookSecret" \
  --value "whsec_..."

az keyvault secret set --vault-name escrow-vault \
  --name "ApiKeys--dev-client-01--Key" \
  --value "..." # API authentication key
```

### Step 5: Create Managed Identity

```bash
# Create managed identity for app (no client secrets needed)
az identity create \
  --resource-group escrow-app-rg \
  --name escrow-app-identity

# Grant access to Key Vault
az keyvault set-policy --vault-name escrow-vault \
  --object-id $(az identity show --name escrow-app-identity -g escrow-app-rg --query principalId -o tsv) \
  --secret-permissions get list
```

### Step 6: Create Container Apps Environment

```bash
# Create managed environment
az containerapp env create \
  --name escrow-env \
  --resource-group escrow-app-rg \
  --location eastus
```

### Step 7: Deploy Container App

```bash
# Create container app with secrets from Key Vault
az containerapp create \
  --name escrow-app \
  --resource-group escrow-app-rg \
  --environment escrow-env \
  --image escrowapp.azurecr.io/escrowapp:1.0.0 \
  --target-port 8080 \
  --ingress 'external' \
  --registry-server escrowapp.azurecr.io \
  --registry-username $(az acr credential show -n escrowapp --query "username" -o tsv) \
  --registry-password $(az acr credential show -n escrowapp --query "passwords[0].value" -o tsv) \
  --cpu 0.5 \
  --memory 1.0Gi \
  --env-vars \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ASPNETCORE_URLS=http://+:8080" \
    "ConnectionStrings__DefaultConnection=@Microsoft.KeyVault(SecretUri=https://escrow-vault.vault.azure.net/secrets/ConnectionStrings--DefaultConnection/)" \
    "Stripe__SecretKey=@Microsoft.KeyVault(SecretUri=https://escrow-vault.vault.azure.net/secrets/Stripe--SecretKey/)" \
    "Stripe__WebhookSecret=@Microsoft.KeyVault(SecretUri=https://escrow-vault.vault.azure.net/secrets/Stripe--WebhookSecret/)" \
  --user-assigned $(az identity show --name escrow-app-identity -g escrow-app-rg --query id -o tsv)

# Get public URL
az containerapp show -n escrow-app -g escrow-app-rg --query properties.configuration.ingress.fqdn
```

### Step 8: Apply Database Migrations

```bash
# Connect to remote database (via Azure Bastion or local tunnel)
# Then run migrations
dotnet ef database update

# Verify schema
psql -h escrow-db.postgres.database.azure.com -U dbadmin -d escrowapp -c "\dt"
```

### Step 9: Configure Stripe Webhooks

```bash
# Get public URL from Step 7
PUBLIC_URL="https://escrow-app.xxx.eastus.azurecontainerapps.io"

# In Stripe Dashboard → Developers → Webhooks:
# Add endpoint: POST ${PUBLIC_URL}/api/webhooks/stripe
# Events: payment_intent.succeeded
# Get webhook secret and update Key Vault
```

### Step 10: Test Health & Readiness

```bash
# Health check
curl https://escrow-app.xxx.eastus.azurecontainerapps.io/health

# Login test
curl -X POST https://escrow-app.xxx.eastus.azurecontainerapps.io/api/login \
  -d '{"email":"test@example.com","password":"password123"}'

# Webhook test (if #7 completed)
stripe trigger payment_intent.succeeded
```

## Alternative Platforms

### AWS ECS/Fargate

```bash
# Create ECR repository
aws ecr create-repository --repository-name escrow-app

# Build & push
docker build -t escrow-app:1.0.0 .
docker tag escrow-app:1.0.0 123456789.dkr.ecr.us-east-1.amazonaws.com/escrow-app:1.0.0
docker push 123456789.dkr.ecr.us-east-1.amazonaws.com/escrow-app:1.0.0

# Create RDS PostgreSQL
aws rds create-db-instance \
  --db-instance-identifier escrow-db \
  --engine postgres \
  --db-instance-class db.t4g.micro \
  --allocated-storage 20

# Create ECS cluster + service (detailed steps omitted)
```

### Google Cloud Run

```bash
# Build & push to Artifact Registry
gcloud builds submit --tag gcr.io/PROJECT_ID/escrow-app

# Deploy to Cloud Run
gcloud run deploy escrow-app \
  --image gcr.io/PROJECT_ID/escrow-app \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production"

# Setup Cloud SQL Proxy for PostgreSQL
gcloud sql instances create escrow-db \
  --database-version POSTGRES_17 \
  --tier db-f1-micro \
  --region us-central1
```

## Post-Launch Verification

### Smoke Tests (Day 1)

```bash
# 1. Health endpoint
curl https://nextruzt.io/health

# 2. Landing page loads
curl -I https://nextruzt.io/

# 3. Register new user
curl -X POST https://nextruzt.io/api/register \
  -d '{"email":"test@example.com","password":"password123"}'

# 4. Login works
curl -X POST https://nextruzt.io/api/login \
  -d '{"email":"test@example.com","password":"password123"}'

# 5. Can create test transaction
curl -X POST https://nextruzt.io/api/transactions \
  -H "Authorization: Bearer ${TOKEN}" \
  -d '{"clientEmail":"client@example.com","consultantEmail":"consultant@example.com","amount":10000,"serviceDescription":"Test"}'

# 6. Webhook signature verification
stripe trigger payment_intent.succeeded --skip-api-check
# Verify logs show "Webhook: Payment intent ... confirmed"
```

### Monitoring Setup (Week 1)

```bash
# Azure Application Insights
az monitor app-insights component create \
  --app escrow-app-insights \
  --location eastus \
  --resource-group escrow-app-rg

# Enable continuous export to Application Insights in Program.cs:
# builder.Services.AddApplicationInsightsTelemetry();
```

### Logging & Alerting

```
# Set up alerts for:
- Container app restart rate > 1/hour
- Database connection pool exhaustion
- Stripe API errors > 5/hour
- 5xx HTTP errors > 10/hour
- Response time p95 > 5s
```

## Rollback Procedure (If Issues)

```bash
# Revert to previous image version
az containerapp update \
  --name escrow-app \
  --resource-group escrow-app-rg \
  --image escrowapp.azurecr.io/escrowapp:0.9.0

# Monitor logs for errors
az containerapp logs show \
  --name escrow-app \
  --resource-group escrow-app-rg \
  --tail 100
```

## Security Hardening — Post-Launch

| Item | Status | When |
|------|--------|------|
| WAF (Web Application Firewall) | 🟡 Planned | Week 2 |
| DDoS protection | 🟡 Planned | Week 2 |
| Backup automation | 🟡 Planned | Week 1 |
| Disaster recovery plan | 🟡 Planned | Week 1 |
| Penetration testing | 🟡 Planned | Pre-launch (4 weeks) |
| Security headers (CSP, HSTS) | ✅ Done | #8 |
| Rate limiting | 🟡 Planned | #7 |

## Cost Estimation (Monthly)

| Service | Tier | Cost |
|---------|------|------|
| Azure Container Apps | 0.5 CPU, 1 GB RAM | ~$50/month |
| PostgreSQL Flexible Server | Burstable B1ms | ~$60/month |
| Key Vault | Pay-per-operation | ~$5/month |
| Application Insights | Ingestion (1GB/day) | ~$30/month |
| Bandwidth (egress) | ~10 GB/month | ~$1/month |
| **Total** | | ~**$146/month** |

> **Note:** Costs scale with transaction volume. First 3 months covered by startup budget.

## Files to Create/Update

| File | Purpose |
|------|---------|
| `.github/workflows/deploy.yml` | CD pipeline (push → ACR → Container Apps) |
| `azure-deploy.sh` | Infrastructure-as-code helper script |
| `docs/operations/deployment-runbook.md` | Day-1 deployment procedures |
| `docs/operations/incident-response.md` | Rollback + emergency procedures |

## Related Documentation

- [Operations → Deployment](deployment.md) — Docker + local dev setup
- [Operations → Health Checks](deployment.md) — Monitoring endpoints
- [Authentication module](../../modules/authentication/aspnet-identity-mvp/aspnet-identity-mvp.md) — Auth configuration
- [Architecture → Stripe Webhooks](../../architecture/stripe-webhooks/minimal-webhook-handler-mvp.md) — Webhook setup
