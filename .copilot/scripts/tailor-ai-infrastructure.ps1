<#
.SYNOPSIS
    Interactive wizard to generate customized AI instruction files for a new project.
.DESCRIPTION
    Asks about your project's tech stack, architecture, and domain, then generates
    tailored AGENTS.md, CLAUDE.md, GEMINI.md, and copilot-instructions.md files.
.PARAMETER OutputPath
    Directory where generated files will be written.
.PARAMETER NonInteractive
    Skip interactive prompts; read configuration from -ConfigFile.
.PARAMETER ConfigFile
    Path to JSON config file with pre-filled answers (for -NonInteractive mode).
.PARAMETER Force
    Overwrite existing files.
.EXAMPLE
    .\tailor-ai-infrastructure.ps1 -OutputPath "C:\Projects\MyNewApp"
.EXAMPLE
    .\tailor-ai-infrastructure.ps1 -OutputPath "C:\Projects\MyApp" -NonInteractive -ConfigFile "config.json"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Directory where generated files will be written.")]
    [string]$OutputPath,

    [Parameter(HelpMessage = "Skip interactive prompts; read configuration from -ConfigFile.")]
    [switch]$NonInteractive,

    [Parameter(HelpMessage = "Path to JSON config file with pre-filled answers.")]
    [string]$ConfigFile,

    [Parameter(HelpMessage = "Overwrite existing files without prompting.")]
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Helper functions
# ---------------------------------------------------------------------------

function Write-Banner {
    param([string]$Text, [ConsoleColor]$Color = 'Cyan')
    $border = '=' * ($Text.Length + 4)
    Write-Host ""
    Write-Host $border -ForegroundColor $Color
    Write-Host "  $Text" -ForegroundColor $Color
    Write-Host $border -ForegroundColor $Color
    Write-Host ""
}

function Write-Section {
    param([string]$Text, [ConsoleColor]$Color = 'Yellow')
    Write-Host ""
    Write-Host "--- $Text ---" -ForegroundColor $Color
    Write-Host ""
}

function Read-PromptValue {
    param(
        [string]$Prompt,
        [string]$Default = '',
        [string[]]$ValidChoices = @(),
        [bool]$Required = $true
    )

    $suffix = if ($Default) { " [$Default]" } else { '' }
    $choiceHint = ''
    if ($ValidChoices.Count -gt 0) {
        $choiceHint = " (" + ($ValidChoices -join ' | ') + ")"
    }

    while ($true) {
        Write-Host "  $Prompt$choiceHint$suffix" -ForegroundColor White -NoNewline
        Write-Host ": " -NoNewline
        $value = Read-Host
        if ([string]::IsNullOrWhiteSpace($value)) {
            if ($Default) { return $Default }
            if (-not $Required) { return '' }
            Write-Host "    -> This field is required. Please enter a value." -ForegroundColor Red
            continue
        }
        if ($ValidChoices.Count -gt 0 -and $value -notin $ValidChoices) {
            Write-Host "    -> Invalid choice. Choose one of: $($ValidChoices -join ', ')" -ForegroundColor Red
            continue
        }
        return $value.Trim()
    }
}

function Read-PromptList {
    param([string]$Prompt, [string]$Default = '')
    $raw = Read-PromptValue -Prompt $Prompt -Default $Default -Required $false
    if ([string]::IsNullOrWhiteSpace($raw)) { return @() }
    return ($raw -split ',').ForEach({ $_.Trim() }).Where({ $_ -ne '' })
}

function Write-FileWithCheck {
    param([string]$Path, [string]$Content)
    if ((Test-Path $Path) -and -not $Force) {
        Write-Host "  [SKIP] $Path already exists. Use -Force to overwrite." -ForegroundColor DarkYellow
        return $false
    }
    $directory = Split-Path $Path -Parent
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    Set-Content -Path $Path -Value $Content -Encoding UTF8
    Write-Host "  [CREATED] $Path" -ForegroundColor Green
    return $true
}

# ---------------------------------------------------------------------------
# Gather configuration
# ---------------------------------------------------------------------------

if ($NonInteractive) {
    if (-not $ConfigFile) {
        Write-Error "In -NonInteractive mode you must supply -ConfigFile."
        return
    }
    if (-not (Test-Path $ConfigFile)) {
        Write-Error "Config file not found: $ConfigFile"
        return
    }
    $cfg = Get-Content $ConfigFile -Raw | ConvertFrom-Json
}
else {
    Write-Banner "AI Infrastructure Tailoring Wizard"
    Write-Host "  This wizard will generate customized AI instruction files" -ForegroundColor Gray
    Write-Host "  (AGENTS.md, CLAUDE.md, GEMINI.md, copilot-instructions.md)" -ForegroundColor Gray
    Write-Host "  tailored to your project." -ForegroundColor Gray

    # --- Project Details ---
    Write-Section "Project Details"
    $projectName        = Read-PromptValue "1. Project Name"
    $projectDescription = Read-PromptValue "2. One-line Description"
    $projectDomain      = Read-PromptValue "3. Domain" -Default "SaaS"
    $targetUsers        = Read-PromptValue "4. Target Users"

    # --- Tech Stack ---
    Write-Section "Tech Stack"
    $primaryLang = Read-PromptValue "5. Primary Language" `
        -ValidChoices @('C#/.NET','TypeScript/Node','Python','Go','Java','Other') `
        -Default 'C#/.NET'
    $frontend = Read-PromptValue "6. Frontend Framework" `
        -ValidChoices @('Blazor Server','Blazor WASM','React','Next.js','Vue','Angular','None') `
        -Default 'None'
    $database = Read-PromptValue "7. Database" `
        -ValidChoices @('PostgreSQL','SQL Server','MySQL','MongoDB','SQLite','None') `
        -Default 'PostgreSQL'
    $orm = Read-PromptValue "8. ORM" `
        -ValidChoices @('EF Core','Dapper','Prisma','TypeORM','Django ORM','SQLAlchemy','None') `
        -Default 'None'
    $authProvider = Read-PromptValue "9. Auth Provider" `
        -ValidChoices @('Entra ID','Auth0','Cognito','Firebase','Custom','None') `
        -Default 'None'

    # --- Architecture ---
    Write-Section "Architecture"
    $archPattern = Read-PromptValue "10. Architecture Pattern" `
        -ValidChoices @('Clean Architecture','Vertical Slice','MVC','Hexagonal','Monolith','Microservices') `
        -Default 'Clean Architecture'
    $cqrs = Read-PromptValue "11. CQRS / Mediator" `
        -ValidChoices @('Yes - MediatR','Yes - Other','No') `
        -Default 'No'
    $designPatterns = Read-PromptList "12. Key Design Patterns (comma-separated)" -Default "Repository,Strategy"

    # --- Project Structure ---
    Write-Section "Project Structure"
    $presentationFolder = Read-PromptValue "13. Presentation Layer Folder" -Default "Components"
    $businessFolder     = Read-PromptValue "14. Business Logic Folder"     -Default "Features"
    $domainFolder       = Read-PromptValue "15. Domain / Models Folder"    -Default "Models"
    $dataFolder         = Read-PromptValue "16. Data Access Folder"        -Default "Data"

    # --- Documentation ---
    Write-Section "Documentation"
    $docsFolder     = Read-PromptValue "17. Docs Folder" -Default "docs"
    $featureAreas   = Read-PromptList  "18. Key Feature Areas (comma-separated)"

    # --- Security ---
    Write-Section "Security"
    $securityFocus = Read-PromptValue "19. Security Focus" `
        -ValidChoices @('OWASP Standard','PCI-DSS/Fintech','HIPAA/Healthcare','SOC2','Standard') `
        -Default 'OWASP Standard'
    $complianceNeeds = Read-PromptValue "20. Special Compliance Needs" -Required $false

    # Build config object
    $cfg = [PSCustomObject]@{
        ProjectName        = $projectName
        ProjectDescription = $projectDescription
        ProjectDomain      = $projectDomain
        TargetUsers        = $targetUsers
        PrimaryLanguage    = $primaryLang
        Frontend           = $frontend
        Database           = $database
        ORM                = $orm
        AuthProvider       = $authProvider
        ArchPattern        = $archPattern
        CQRS               = $cqrs
        DesignPatterns      = $designPatterns
        PresentationFolder = $presentationFolder
        BusinessFolder     = $businessFolder
        DomainFolder       = $domainFolder
        DataFolder         = $dataFolder
        DocsFolder         = $docsFolder
        FeatureAreas       = $featureAreas
        SecurityFocus      = $securityFocus
        ComplianceNeeds    = $complianceNeeds
    }
}

# Validate required fields from config
$requiredFields = @('ProjectName','ProjectDescription','ProjectDomain','TargetUsers','PrimaryLanguage')
foreach ($field in $requiredFields) {
    $val = $cfg.$field
    if ([string]::IsNullOrWhiteSpace($val)) {
        Write-Error "Required field '$field' is empty."
        return
    }
}

# Ensure OutputPath exists
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# ---------------------------------------------------------------------------
# Derived values — language-specific defaults
# ---------------------------------------------------------------------------

$lang = $cfg.PrimaryLanguage
$fe   = $cfg.Frontend
$db   = $cfg.Database
$ormChoice = $cfg.ORM
$arch = $cfg.ArchPattern
$useCqrs = $cfg.CQRS -like 'Yes*'
$mediatorLib = if ($cfg.CQRS -eq 'Yes - MediatR') { 'MediatR' } elseif ($cfg.CQRS -eq 'Yes - Other') { 'mediator library' } else { '' }

# File extensions for applyTo patterns
$codeExt = switch -Wildcard ($lang) {
    'C#/.NET'          { '*.cs' }
    'TypeScript/Node'  { '*.ts' }
    'Python'           { '*.py' }
    'Go'               { '*.go' }
    'Java'             { '*.java' }
    default            { '*.*' }
}

$markupExt = switch -Wildcard ($fe) {
    'Blazor*'   { '*.razor' }
    'React'     { '*.tsx' }
    'Next.js'   { '*.tsx' }
    'Vue'       { '*.vue' }
    'Angular'   { '*.html' }
    default     { '' }
}

$applyToCode = "**/$codeExt"
if ($markupExt) { $applyToCode += ", **/$markupExt" }

# Code conventions block
$codeConventions = switch -Wildcard ($lang) {
    'C#/.NET' {
@"
## Code Conventions

| Convention | Rule |
|---|---|
| Namespaces | File-scoped (``namespace X;``) |
| Nullability | Enabled — use ``string?`` for nullable |
| Inheritance | ``sealed`` by default on concrete classes |
| DTOs | ``record`` types with ``init`` properties |
| Async | ``async Task`` / ``async Task<T>`` with ``CancellationToken`` |
| Naming | Intention-revealing. No abbreviations except DTO, ID, HTTP |
| Guard clauses | Fail fast at method entry — no deep nesting |
| Constants | No magic strings or numbers — use ``const`` or ``enum`` |
| Members | Prefer expression-bodied members for single-line logic |
| Collections | Return ``IReadOnlyCollection<T>`` / ``IReadOnlyList<T>`` |
"@
    }
    'TypeScript/Node' {
@"
## Code Conventions

| Convention | Rule |
|---|---|
| Modules | ESM (``import`` / ``export``) — no CommonJS ``require`` |
| Types | Strict mode (``strict: true`` in tsconfig). Always type function parameters and returns |
| Immutability | Prefer ``readonly``, ``as const``, ``Readonly<T>`` |
| Interfaces | Interface-first design — define contracts before implementations |
| Async | ``async`` / ``await`` — never raw ``Promise.then()`` chains |
| Naming | camelCase for variables/functions, PascalCase for types/classes |
| Validation | Zod schemas at API boundaries |
| Error handling | Use typed error classes, never throw plain strings |
| Nullability | Prefer ``undefined`` over ``null``; use optional chaining (``?.``) |
| Collections | Prefer ``ReadonlyArray<T>`` and ``ReadonlyMap`` for returned data |
"@
    }
    'Python' {
@"
## Code Conventions

| Convention | Rule |
|---|---|
| Type hints | All function signatures fully typed (``def func(x: int) -> str``) |
| Data classes | Use ``@dataclass`` or Pydantic ``BaseModel`` for data transfer objects |
| Async | ``async`` / ``await`` with ``asyncio`` for I/O-bound work |
| Naming | ``snake_case`` for functions/variables, ``PascalCase`` for classes |
| Imports | Absolute imports, sorted with ``isort``, formatted with ``black`` |
| Validation | Pydantic models at API boundaries |
| Error handling | Custom exception classes inheriting from domain-specific base |
| Constants | ``UPPER_SNAKE_CASE`` module-level constants |
| Immutability | Prefer ``frozen=True`` on dataclasses, ``tuple`` over ``list`` for fixed data |
| Docstrings | Google style docstrings on public functions and classes |
"@
    }
    'Go' {
@"
## Code Conventions

| Convention | Rule |
|---|---|
| Packages | Short, lowercase, single-word names |
| Errors | Return ``error`` as last value; wrap with ``fmt.Errorf("context: %w", err)`` |
| Interfaces | Accept interfaces, return structs — keep interfaces small |
| Naming | MixedCaps / mixedCaps. Exported = PascalCase, unexported = camelCase |
| Tests | Table-driven tests with ``t.Run()`` sub-tests |
| Concurrency | Use channels and ``context.Context``; propagate cancellation |
| Error handling | Handle every ``error`` — no ``_`` discard on error returns |
| Validation | Validate at handler/transport boundary before calling business logic |
| Structs | Prefer value types; use pointers only when mutation or nil semantics needed |
| Linting | ``golangci-lint`` with ``govet``, ``staticcheck``, ``errcheck`` enabled |
"@
    }
    'Java' {
@"
## Code Conventions

| Convention | Rule |
|---|---|
| Records | Use ``record`` for DTOs and value objects (Java 16+) |
| Sealed | Use ``sealed`` classes/interfaces where inheritance is constrained |
| Nullability | Use ``Optional<T>`` for return types; ``@Nullable`` / ``@NonNull`` annotations |
| Async | ``CompletableFuture`` for async operations |
| Naming | camelCase for methods/fields, PascalCase for classes, UPPER_SNAKE for constants |
| Streams | Prefer Stream API for collection transformations |
| Immutability | Prefer ``List.of()``, ``Map.of()``, unmodifiable collections |
| Validation | Bean Validation (``@Valid``, ``@NotNull``) at controller boundaries |
| Error handling | Custom exceptions extending domain-specific base; never catch ``Exception`` broadly |
| Logging | SLF4J with structured parameters — never string concatenation in log calls |
"@
    }
    default {
@"
## Code Conventions

| Convention | Rule |
|---|---|
| Naming | Intention-revealing names; consistent casing per language norms |
| Immutability | Prefer immutable data structures where possible |
| Async | Use async/await or equivalent for I/O-bound operations |
| Validation | Validate all input at boundaries |
| Error handling | Use typed errors; never swallow exceptions silently |
| Constants | No magic strings or numbers |
"@
    }
}

# Security section based on focus
$securitySection = switch ($cfg.SecurityFocus) {
    'PCI-DSS/Fintech' {
@"
## Security — PCI-DSS & OWASP Top 10

| Category | Requirement |
|---|---|
| **Broken Access Control (A01)** | ``[Authorize]`` / auth middleware on every endpoint. Policy-based auth. Default deny. |
| **Cryptographic Failures (A02)** | Secrets via env vars or Key Vault. Never in source or config. Encrypt PII at rest. |
| **Injection (A03)** | Parameterized queries only. No string concatenation in SQL/commands. |
| **Insecure Design (A04)** | Threat model all payment flows. Defense in depth. |
| **Security Misconfiguration (A05)** | HTTPS + HSTS enforced. Antiforgery tokens. No debug in prod. |
| **Vulnerable Components (A06)** | Dependency scanning in CI. Monitor CVEs. |
| **Auth Failures (A07)** | MFA for privileged ops. Token validation. Session management. |
| **Data Integrity (A08)** | Signed payloads. Verify webhook signatures. |
| **Logging Failures (A09)** | Structured logging. Correlation IDs. **Never log PII, tokens, or secrets.** |
| **SSRF (A10)** | Validate external URLs. Allowlist outbound calls. |

### PCI-DSS Specific

- **Never** store raw card numbers, CVVs, or full magnetic stripe data.
- Delegate payment processing to certified providers (Stripe, Adyen, etc.) — tokenized references only.
- Audit log all payment operations with timestamps, actor identity, and idempotency keys.
- Idempotency keys on **every** payment mutation. All payment operations must be safely retryable.
- Use manual capture / two-phase commit for payment authorization and capture.
"@
    }
    'HIPAA/Healthcare' {
@"
## Security — HIPAA & OWASP Top 10

| Category | Requirement |
|---|---|
| **Broken Access Control (A01)** | Role-based + attribute-based access control. Minimum necessary access. Default deny. |
| **Cryptographic Failures (A02)** | Encrypt PHI at rest (AES-256) and in transit (TLS 1.2+). Key rotation. |
| **Injection (A03)** | Parameterized queries only. Input validation on all boundaries. |
| **Insecure Design (A04)** | Threat model all data flows involving PHI. Privacy by design. |
| **Security Misconfiguration (A05)** | HTTPS + HSTS. No debug in prod. Audit configurations regularly. |
| **Vulnerable Components (A06)** | Dependency scanning. Patch management within 30 days for critical CVEs. |
| **Auth Failures (A07)** | MFA required. Session timeout after inactivity. Unique user IDs. |
| **Data Integrity (A08)** | Integrity verification on PHI transfers. Tamper-evident audit logs. |
| **Logging Failures (A09)** | Comprehensive audit trail. Log access to PHI. **Never log PHI in plaintext.** |
| **SSRF (A10)** | Validate external URLs. Network segmentation for PHI systems. |

### HIPAA Specific

- Implement **access audit logs** for all PHI access — who, what, when, from where.
- Support **Break-the-Glass** emergency access with post-access review.
- Data retention and disposal policies enforced in code.
- BAA (Business Associate Agreement) compliance for all third-party services.
- Minimum necessary rule: return only the PHI fields required for the operation.
"@
    }
    'SOC2' {
@"
## Security — SOC 2 & OWASP Top 10

| Category | Requirement |
|---|---|
| **Broken Access Control (A01)** | RBAC with principle of least privilege. Default deny. Periodic access reviews. |
| **Cryptographic Failures (A02)** | Secrets in vault. Encrypt sensitive data at rest and in transit. |
| **Injection (A03)** | Parameterized queries only. Input sanitization. |
| **Insecure Design (A04)** | Secure SDLC. Threat modeling for new features. |
| **Security Misconfiguration (A05)** | HTTPS + HSTS. Hardened configurations. Change management. |
| **Vulnerable Components (A06)** | Automated dependency scanning. SLA for patching. |
| **Auth Failures (A07)** | MFA. Password complexity requirements. Account lockout. |
| **Data Integrity (A08)** | Change detection. Immutable audit logs. |
| **Logging Failures (A09)** | Centralized logging. Retention policies. Alerting on anomalies. |
| **SSRF (A10)** | Network segmentation. Outbound traffic filtering. |

### SOC 2 Specific

- **Availability:** Health checks, uptime monitoring, incident response procedures.
- **Confidentiality:** Data classification. Encrypt confidential data. Access controls.
- **Processing Integrity:** Input validation. Reconciliation checks. Error handling.
- **Privacy:** Consent management. Data minimization. Right to deletion.
- Audit trail for all administrative and data-mutating operations.
"@
    }
    default {
@"
## Security — OWASP Top 10

| Category | Requirement |
|---|---|
| **Broken Access Control (A01)** | Auth middleware on every endpoint. Policy-based authorization. Default deny. |
| **Cryptographic Failures (A02)** | Secrets via env vars or secret manager. Never in source or config. |
| **Injection (A03)** | Parameterized queries only. No string concatenation in SQL/commands. |
| **Insecure Design (A04)** | Review threat model for new features. |
| **Security Misconfiguration (A05)** | HTTPS + HSTS enforced. Antiforgery tokens on state-changing requests. |
| **Vulnerable Components (A06)** | Keep dependencies updated. Scan for CVEs in CI. |
| **Auth Failures (A07)** | Validate tokens. Session management. Brute-force protection. |
| **Data Integrity (A08)** | Verify deserialization safety. Signed artifacts. |
| **Logging Failures (A09)** | Structured logging. Correlation IDs. **Never log secrets or PII.** |
| **SSRF (A10)** | Validate external URLs. Allowlist outbound calls. |
"@
    }
}

# Architecture layer map
$layerMap = @"
### Layer Map

``````
Presentation    ->  $($cfg.PresentationFolder)/    UI pages, layouts, styles
Application     ->  $($cfg.BusinessFolder)/        Business logic, handlers$(if($useCqrs){", CQRS slices"})
Domain          ->  $($cfg.DomainFolder)/          Entities, value objects, domain events, interfaces
Infrastructure  ->  $($cfg.DataFolder)/            Data access, external service integrations
``````
"@

$dependencyDirection = @"
### Dependency Direction — MANDATORY

``````
$($cfg.PresentationFolder)/ --> $($cfg.BusinessFolder)/ --> $($cfg.DomainFolder)/    <-- $($cfg.DataFolder)/
``````

Inner layers ($($cfg.DomainFolder)/) **never** reference outer layers. Infrastructure implements domain interfaces.
"@

# Design patterns table
$dpRows = @()
if ($cfg.DesignPatterns -and $cfg.DesignPatterns.Count -gt 0) {
    foreach ($p in $cfg.DesignPatterns) {
        $desc = switch ($p.Trim().ToLower()) {
            'repository'  { "Abstract data access behind interfaces." }
            'strategy'    { "Swap behaviors (e.g., payment providers) at runtime." }
            'factory'     { "Encapsulate complex object creation." }
            'decorator'   { "Add cross-cutting concerns (logging, caching) without modifying core logic." }
            'mediator'    { "Decouple request handling via command/query dispatching." }
            'observer'    { "Publish/subscribe for domain events and notifications." }
            'specification' { "Encapsulate query criteria as composable objects." }
            'builder'     { "Construct complex objects step-by-step." }
            'cqrs'        { "Separate read and write models for scalability." }
            'saga'        { "Coordinate multi-step distributed transactions." }
            'event sourcing' { "Persist state as a sequence of domain events." }
            'unit of work' { "Coordinate multiple repository operations in a single transaction." }
            default       { "Applied where appropriate to reduce complexity." }
        }
        $dpRows += "| **$($p.Trim())** | $desc |"
    }
}
$designPatternsTable = if ($dpRows.Count -gt 0) {
@"
## Design Patterns

| Pattern | Purpose |
|---|---|
$($dpRows -join "`n")

Do not over-engineer — apply patterns only when they reduce complexity.
"@
} else {
@"
## Design Patterns

Apply design patterns when they reduce complexity. Avoid premature abstraction — see the Rule of Three.
"@
}

# CQRS section
$cqrsSection = if ($useCqrs) {
@"
## CQRS $(if($mediatorLib){"& $mediatorLib "})— MANDATORY

All business operations go through $(if($mediatorLib){$mediatorLib}else{"the mediator"}) handlers in ``$($cfg.BusinessFolder)/``.

**Rules:**
- UI components and API controllers dispatch commands/queries via the mediator — never call services directly.
- Handlers orchestrate: validate -> execute -> persist -> publish domain events.
- Separate command (write) and query (read) paths for clarity and scalability.

### Vertical Slice Structure

Each use case is a self-contained slice:

``````
$($cfg.BusinessFolder)/
└── {FeatureName}/
    ├── {Action}Command.cs       <- Command/Query definition
    ├── {Action}Handler.cs       <- Handler implementation
    ├── {Action}Validator.cs     <- Input validation
    └── {Action}Result.cs        <- Response DTO
``````
"@
} else { '' }

# Feature areas docs table
$featureDocRows = @()
if ($cfg.FeatureAreas -and $cfg.FeatureAreas.Count -gt 0) {
    foreach ($area in $cfg.FeatureAreas) {
        $slug = ($area.Trim().ToLower() -replace '\s+', '-')
        $featureDocRows += "| ``$($cfg.DocsFolder)/features/$slug`` | $($area.Trim()) |"
    }
}
$featureDocsTable = if ($featureDocRows.Count -gt 0) {
    ($featureDocRows -join "`n")
} else {
    "| ``$($cfg.DocsFolder)/features/`` | Feature documentation (create as needed) |"
}

# Auth section
$authSection = switch ($cfg.AuthProvider) {
    'Entra ID' {
@"
## Authentication & Authorization

- **Provider:** Microsoft Entra ID (Azure AD) via ``Microsoft.Identity.Web``.
- Use OIDC for interactive flows; JWT Bearer tokens for API-to-API.
- Register apps with least-privilege API permissions.
- Use **App Roles** for coarse-grained authorization; map to policies in code.
- Prefer **Managed Identity** for Azure-hosted services.
- Store ``ClientSecret`` in Azure Key Vault — never in config files.
- Policy-based authorization (``[Authorize(Policy = "...")]``) over role checks.
- Default posture: deny all, allow explicitly.
"@
    }
    'Auth0' {
@"
## Authentication & Authorization

- **Provider:** Auth0.
- Use OIDC + PKCE for interactive flows; JWT for APIs.
- Define API permissions as Auth0 scopes; map to application policies.
- Store Auth0 domain, client ID, and client secret in a secret manager.
- Validate JWT: issuer, audience, lifetime, signing key.
- Policy-based authorization over role checks.
- Default posture: deny all, allow explicitly.
"@
    }
    'Cognito' {
@"
## Authentication & Authorization

- **Provider:** AWS Cognito.
- Use Cognito User Pools for authentication; Cognito Identity Pools for AWS resource access.
- Store pool IDs and client secrets in AWS Secrets Manager or Parameter Store.
- Validate JWT tokens from Cognito; check issuer and audience claims.
- Policy-based authorization mapped from Cognito groups.
- Default posture: deny all, allow explicitly.
"@
    }
    'Firebase' {
@"
## Authentication & Authorization

- **Provider:** Firebase Authentication.
- Use Firebase Admin SDK for server-side token verification.
- Store Firebase service account credentials securely — never in source code.
- Validate Firebase ID tokens on every API request.
- Map Firebase custom claims to application-level policies.
- Default posture: deny all, allow explicitly.
"@
    }
    'Custom' {
@"
## Authentication & Authorization

- **Provider:** Custom authentication implementation.
- Use industry-standard algorithms (bcrypt/argon2 for password hashing, JWT or opaque tokens).
- Never store plaintext passwords. Enforce password complexity.
- Validate tokens on every request: check signature, expiry, issuer, audience.
- Policy-based authorization — centralize policy definitions.
- Default posture: deny all, allow explicitly.
"@
    }
    default {
@"
## Authentication & Authorization

- Add an authentication provider before going to production.
- When added, use policy-based authorization — never inline role strings.
- Default posture: deny all, allow explicitly.
"@
    }
}

# ORM / data access section
$dataAccessSection = switch -Wildcard ($ormChoice) {
    'EF Core' {
@"
## Data Access — Entity Framework Core

- One ``DbContext`` registered as scoped.
- Apply entity configurations via ``IEntityTypeConfiguration<T>``.
- Read queries: ``AsNoTracking()`` for performance.
- Never expose ``IQueryable<T>`` from repositories.
- Create migrations for schema changes: ``dotnet ef migrations add MigrationName``.
- Repository interfaces in the Domain layer; implementations in Infrastructure.
"@
    }
    'Dapper' {
@"
## Data Access — Dapper

- Use parameterized queries exclusively — never string-concatenate input.
- Wrap multi-statement operations in explicit ``IDbTransaction``.
- Repository interfaces in the Domain layer; implementations in Infrastructure.
- Use ``record`` types for query result mapping.
"@
    }
    'Prisma' {
@"
## Data Access — Prisma

- Define schema in ``prisma/schema.prisma``.
- Generate client after schema changes: ``npx prisma generate``.
- Use migrations: ``npx prisma migrate dev --name migration_name``.
- Repository pattern: abstract Prisma client behind interfaces.
- Never expose the Prisma client directly to handlers or controllers.
"@
    }
    'TypeORM' {
@"
## Data Access — TypeORM

- Use repository pattern — abstract TypeORM behind interfaces.
- Define entities with decorators; validate column types match database.
- Use migrations for schema changes.
- Read queries: use ``QueryBuilder`` with parameterized conditions.
"@
    }
    'Django ORM' {
@"
## Data Access — Django ORM

- Define models in ``models.py``; use Django migrations for schema changes.
- Use ``QuerySet`` API — never raw SQL unless performance-critical.
- If raw SQL needed, use parameterized queries via ``cursor.execute(sql, params)``.
- Repository/service layer abstracts ORM access from views.
"@
    }
    'SQLAlchemy' {
@"
## Data Access — SQLAlchemy

- Use declarative models with ``mapped_column()``.
- Session management: scoped sessions per request.
- Use Alembic for migrations: ``alembic revision --autogenerate``.
- Repository interfaces abstract SQLAlchemy from business logic.
- Parameterized queries only — never f-string SQL.
"@
    }
    default {
@"
## Data Access

- Abstract data access behind repository interfaces.
- Use parameterized queries — never concatenate user input into queries.
- Define data access interfaces in the Domain layer; implementations in Infrastructure.
"@
    }
}

# ---------------------------------------------------------------------------
# Generate AGENTS.md
# ---------------------------------------------------------------------------

$agentsMd = @"
# AGENTS.md — $($cfg.ProjectName)

> Universal instructions for all AI coding agents working on this repository.

## Project Identity

**$($cfg.ProjectName)** — $($cfg.ProjectDescription)

- **Domain:** $($cfg.ProjectDomain)
- **Users:** $($cfg.TargetUsers)
- **Tech Stack:** $lang$(if($fe -ne 'None'){", $fe"})$(if($db -ne 'None'){", $db"})$(if($ormChoice -ne 'None'){", $ormChoice"})$(if($useCqrs){", $mediatorLib"})
- **Architecture:** $arch

---

## Architecture Overview

This project follows **$arch**$(if($useCqrs){" with **CQRS via $mediatorLib**"}).

$layerMap

$dependencyDirection

---

$designPatternsTable

---

$cqrsSection

$codeConventions

---

$securitySection

---

$authSection

---

$dataAccessSection

---

## Documentation — MANDATORY

The ``$($cfg.DocsFolder)/`` directory contains documentation organized by concern:

| Path | Topic |
|---|---|
| ``$($cfg.DocsFolder)/architecture/overview`` | System design, layers, dependency rules |
$featureDocsTable
| ``$($cfg.DocsFolder)/cross-cutting/`` | Cross-cutting concerns (auth, logging, etc.) |
| ``$($cfg.DocsFolder)/operations/`` | Deployment, CI/CD, monitoring |

**When you add or change a feature, update the corresponding doc.** If no doc exists for a new feature, create one under the appropriate concern category.

---

## Skills Catalog

Reusable AI skills at ``.github/skills/`` — organized by category.

### How to Use a Skill (Any Model)

``````bash
# Step 1: Find the right skill
cat .github/skills/CATALOG.md

# Step 2: Read the skill core file
cat .github/skills/{category}/{skill-name}/SKILL.md

# Step 3: Follow the Core Workflow inside

# Step 4: Load references on demand
cat .github/skills/{category}/{skill-name}/references/{topic}.md
``````

### Rules

- **Read, don't invoke** — skills are files, not tools.
- **One skill at a time** — only read the skill matching the current task.
- **Progressive disclosure** — never load all references; pick the relevant one.
- **Follow checkpoints** — each Core Workflow step has a checkpoint; verify before proceeding.
"@

# ---------------------------------------------------------------------------
# Generate CLAUDE.md
# ---------------------------------------------------------------------------

$claudeCodeGenRules = switch -Wildcard ($lang) {
    'C#/.NET' {
@"
### C# Code

- **File-scoped namespaces.** Always ``namespace X;`` — never block-scoped.
- **Nullable enabled.** Use ``string?`` for nullable, never ``string`` for potentially null values.
- **Sealed by default.** Add ``sealed`` to classes not designed for inheritance.
- **Records for DTOs.** Commands, queries, and response models should be ``record`` types.
- **Primary constructors** for simple DI injection in handlers.
- **Cancellation tokens.** Every async method accepts and propagates ``CancellationToken``.
- **Explicit type annotations** for domain types. ``var`` acceptable for obvious types.
"@
    }
    'TypeScript/Node' {
@"
### TypeScript Code

- **Strict mode.** ``strict: true`` in tsconfig — no implicit any.
- **Interface-first.** Define interfaces before implementations.
- **Readonly by default.** Use ``readonly`` on properties, ``Readonly<T>`` for objects.
- **Use ``type`` for unions/intersections**, ``interface`` for object shapes.
- **Async/await.** Never use raw ``.then()`` chains.
- **Zod for validation** at API boundaries.
- **ESM imports.** No CommonJS ``require()``.
"@
    }
    'Python' {
@"
### Python Code

- **Full type hints.** Every function signature typed: ``def func(x: int) -> str:``
- **Pydantic models** at API boundaries for validation and serialization.
- **Dataclasses** for internal data structures (``@dataclass(frozen=True)`` when immutable).
- **Async/await** for I/O-bound operations.
- **Google-style docstrings** on public functions and classes.
- **Path handling** via ``pathlib.Path``, not ``os.path``.
"@
    }
    'Go' {
@"
### Go Code

- **Accept interfaces, return structs.**
- **Wrap errors** with context: ``fmt.Errorf("doing X: %w", err)``.
- **Table-driven tests** with ``t.Run()`` sub-tests.
- **Context propagation.** Pass ``context.Context`` as the first parameter.
- **Small interfaces.** 1-3 methods per interface.
- **No init() functions** unless absolutely necessary.
"@
    }
    'Java' {
@"
### Java Code

- **Records for DTOs** (Java 16+).
- **Sealed classes** where inheritance is constrained.
- **Optional<T>** for nullable return types.
- **Stream API** for collection transformations.
- **SLF4J** with structured logging parameters — never string concatenation.
- **Bean Validation** (``@Valid``, ``@NotNull``) at controller boundaries.
"@
    }
    default {
@"
### General Code

- Use the language's strongest typing features.
- Prefer immutable data structures.
- Validate all input at boundaries.
- Handle errors explicitly — never swallow silently.
"@
    }
}

# Claude feature-to-doc mapping
$claudeDocRows = @()
if ($cfg.FeatureAreas -and $cfg.FeatureAreas.Count -gt 0) {
    foreach ($area in $cfg.FeatureAreas) {
        $slug = ($area.Trim().ToLower() -replace '\s+','-')
        $claudeDocRows += "| $($area.Trim()) changes | ``$($cfg.DocsFolder)/features/$slug`` |"
    }
}
$claudeDocRows += "| Architecture changes | ``$($cfg.DocsFolder)/architecture/overview`` |"
$claudeDocRows += "| Auth / identity changes | ``$($cfg.DocsFolder)/cross-cutting/auth`` |"
$claudeDocRows += "| Deployment changes | ``$($cfg.DocsFolder)/operations/deployment`` |"
$claudeDocTable = $claudeDocRows -join "`n"

$claudeMd = @"
# CLAUDE.md — Claude-Specific Instructions for $($cfg.ProjectName)

> These instructions extend AGENTS.md with guidance optimized for Claude's reasoning capabilities.

## Project Context

Read **AGENTS.md** first for full project context. This file adds Claude-specific guidance for:

- Structured reasoning about architecture decisions
- Step-by-step analysis during refactoring
- Chain-of-thought for complex design
- Security review methodology

---

## Reasoning Approach

### Architectural Decisions

When making architectural decisions, reason through this checklist:

1. **Which layer does this belong to?** Map the change to Presentation / Application / Domain / Infrastructure.
2. **Does it violate dependency direction?** Inner layers must never reference outer layers.
3. **Which pattern applies?** Match against the project's established patterns.
4. **What are the SOLID implications?**
   - SRP: Does this class have one reason to change?
   - OCP: Can this be extended without modifying existing code?
   - LSP: Are subtypes substitutable?
   - ISP: Is the interface focused?
   - DIP: Are we depending on abstractions?

### Refactoring

When refactoring existing code, think step-by-step:

1. **Identify the smell.** Name the specific code smell or violation.
2. **Trace dependencies.** Map what depends on the code being changed.
3. **Evaluate SOLID impact.** Which principles are violated? Which will the refactoring satisfy?
4. **Plan the migration.** Ensure backward compatibility where needed.
5. **Verify invariants.** After refactoring, do business rules still hold?

---

## Code Generation Rules

$claudeCodeGenRules

---

## Security Review Methodology

When reviewing code for security, systematically evaluate each OWASP Top 10 category:

| # | Category | What to Check |
|---|---|---|
| A01 | Broken Access Control | Auth on every endpoint? Policy-based? Default deny? |
| A02 | Cryptographic Failures | Secrets in code? PII in logs? TLS enforced? |
| A03 | Injection | Parameterized queries? No string concatenation in queries? |
| A04 | Insecure Design | Threat model reviewed? Business logic bypasses? |
| A05 | Security Misconfiguration | HTTPS? HSTS? Debug disabled in prod? |
| A06 | Vulnerable Components | Dependencies up to date? Known CVEs? |
| A07 | Auth Failures | Token validation? Brute-force protection? |
| A08 | Data Integrity Failures | Deserialization safe? Pipeline integrity? |
| A09 | Logging Failures | Audit trail? Correlation IDs? No secrets in logs? |
| A10 | SSRF | External URL validation? Allowlisting? |

For each finding, provide: **Severity** (Critical/High/Medium/Low), **Location**, **Issue**, **Fix**.

---

## Documentation Updates

When modifying features, Claude must check and update the corresponding doc:

| Feature Area | Doc to Update |
|---|---|
$claudeDocTable

If no doc exists for a new feature, create one under the appropriate concern category in ``$($cfg.DocsFolder)/``.

---

## Immutability Preferences

Favor immutable constructs wherever possible:

- Immutable data transfer objects (records, frozen dataclasses, readonly interfaces)
- Read-only collections for return types
- Sealed / final classes to prevent unintended inheritance
- Expression-bodied / single-expression members for simple logic

---

## Error Handling Guidance

- Use domain-specific exceptions for business rule violations.
- Handlers catch infrastructure exceptions and translate to meaningful domain errors.
- Never swallow exceptions silently — log with context and correlation IDs.
- Return appropriate status codes: 400 for validation, 404 for not found, 409 for conflicts, 500 for unexpected.
"@

# ---------------------------------------------------------------------------
# Generate GEMINI.md
# ---------------------------------------------------------------------------

$geminiCrossRef = @"
### Cross-Referencing Checklist

When exploring the codebase:

| Question | Where to Look |
|---|---|
| How is DI wired? | Entry point / composition root (e.g., ``Program.cs``, ``main.ts``, ``app.py``) |
| What business operations exist? | ``$($cfg.BusinessFolder)/`` — each subdirectory or module |
| What's the data schema? | ``$($cfg.DomainFolder)/`` entities, ``$($cfg.DataFolder)/`` context/migrations |
| What API endpoints exist? | Controllers, route handlers, or API modules |
$(if($cfg.FeatureAreas -and $cfg.FeatureAreas.Count -gt 0) {
    ($cfg.FeatureAreas | ForEach-Object { "| How does $($_.Trim()) work? | ``$($cfg.BusinessFolder)/`` + ``$($cfg.DocsFolder)/features/$($_.Trim().ToLower() -replace '\s+','-')`` |" }) -join "`n"
})
"@

$geminiMd = @"
# GEMINI.md — Gemini-Specific Instructions for $($cfg.ProjectName)

> These instructions extend AGENTS.md with guidance optimized for Gemini's analysis and code generation capabilities.

## Project Context

Read **AGENTS.md** first for full project context. This file adds Gemini-specific guidance for:

- Dependency graph analysis before changes
- Pattern matching against existing codebase conventions
- Efficient code search and cross-referencing
- Data access and query generation

---

## Exploration Strategy

### Before Making Changes — Map Dependencies First

When asked to modify any code, analyze the dependency graph before writing:

1. **Trace inbound references.** What calls/imports the file being changed?
2. **Trace outbound references.** What does the file depend on?
3. **Identify the layer.** Presentation -> Application -> Domain <- Infrastructure.
4. **Check for pattern consistency.** How do similar files in the same directory handle this?
5. **Verify interface contracts.** If changing an interface, identify all implementations and consumers.

$geminiCrossRef

---

## Code Generation Guidelines

### Match Existing Patterns

Before generating code, find and match the project's established patterns:

1. **Search for a similar file** in the same directory.
2. **Copy its structure** — imports, naming, error handling, patterns.
3. **Verify DI registration** — new services must be registered in the composition root.
4. **Check for existing tests** — match the testing pattern for new code.

### Code Style Rules

Match the project's code conventions (see AGENTS.md Code Conventions section).
When in doubt, follow the style of the nearest existing file in the same directory.

---

## Feature Modification Workflow

When adding or modifying a feature:

``````
1. Identify the relevant module in $($cfg.BusinessFolder)/
2. Check the corresponding doc in $($cfg.DocsFolder)/ (if it exists)
3. Map dependencies (data access, external services, events)
4. Make changes following existing patterns
5. Update the docs entry (or create one)
6. Verify DI registration if new services are added
7. Run existing tests to verify no regressions
``````

---

## UI Component Analysis

When working with UI components:

1. **Inspect all related files** — markup, logic, and styles.
2. **Check parent-child relationships** — props, events, slots/children.
3. **Verify accessibility** — ARIA attributes, keyboard navigation, screen reader support.
4. **Match existing styling patterns** — use the project's CSS framework consistently.

---

## Database & Data Access

$dataAccessSection

### Before Writing Queries

1. Check existing repository methods — avoid duplicating functionality.
2. Examine the data context/schema for configured relationships and indexes.
3. Match existing query patterns in the codebase.
4. Check for existing migrations before creating new ones.

---

## Documentation Maintenance

When features change, update the corresponding doc in ``$($cfg.DocsFolder)/``.
The documentation structure mirrors the project:

``````
$($cfg.DocsFolder)/
├── architecture/     <- cross-cutting architecture decisions
├── features/         <- feature-specific documentation
├── cross-cutting/    <- auth, logging, localization, testing
├── operations/       <- deployment, CI/CD, monitoring
└── planning/         <- project execution tracking
``````

New features that don't fit existing docs: create a folder under the appropriate concern category.
"@

# ---------------------------------------------------------------------------
# Generate copilot-instructions.md
# ---------------------------------------------------------------------------

$copilotInstructions = @"
# Copilot Instructions — $($cfg.ProjectName)

> Project-level instructions for GitHub Copilot and all AI coding assistants.

## Project

**$($cfg.ProjectName)** — $($cfg.ProjectDescription)

**Tech Stack:**
- $lang$(if($fe -ne 'None'){"`n- $fe"})$(if($db -ne 'None'){"`n- $db"})$(if($ormChoice -ne 'None'){"`n- $ormChoice"})$(if($useCqrs){"`n- $mediatorLib (CQRS)"})$(if($cfg.AuthProvider -ne 'None'){"`n- $($cfg.AuthProvider) (Auth)"})

---

## Architecture

**$arch**$(if($useCqrs){" with **CQRS**"}) organized as$(if($useCqrs){" vertical slices"} else {" layered modules"}).

$layerMap

$dependencyDirection

---

$codeConventions

---

$securitySection

---

$authSection

---

$dataAccessSection

---

## Documentation

Update ``$($cfg.DocsFolder)/`` when features change. If no doc exists for a new feature, create one under the appropriate concern category.
"@

# Also build a .github/copilot-instructions.md compatible version
$copilotGhInstructions = @"
---
applyTo: "$applyToCode"
---

$copilotInstructions
"@

# ---------------------------------------------------------------------------
# Write files
# ---------------------------------------------------------------------------

Write-Banner "Generating Files" -Color Green

$filesWritten = @()

$result = Write-FileWithCheck -Path (Join-Path $OutputPath 'AGENTS.md') -Content $agentsMd
if ($result) { $filesWritten += 'AGENTS.md' }

$result = Write-FileWithCheck -Path (Join-Path $OutputPath 'CLAUDE.md') -Content $claudeMd
if ($result) { $filesWritten += 'CLAUDE.md' }

$result = Write-FileWithCheck -Path (Join-Path $OutputPath 'GEMINI.md') -Content $geminiMd
if ($result) { $filesWritten += 'GEMINI.md' }

# Write copilot-instructions.md to .github/ if it exists, otherwise to OutputPath
$ghDir = Join-Path $OutputPath '.github'
if (-not (Test-Path $ghDir)) {
    New-Item -ItemType Directory -Path $ghDir -Force | Out-Null
}
$result = Write-FileWithCheck -Path (Join-Path $ghDir 'copilot-instructions.md') -Content $copilotGhInstructions
if ($result) { $filesWritten += '.github/copilot-instructions.md' }

# ---------------------------------------------------------------------------
# Save config JSON
# ---------------------------------------------------------------------------

$configPath = Join-Path $OutputPath '.ai-infrastructure-config.json'
$cfg | ConvertTo-Json -Depth 5 | Set-Content -Path $configPath -Encoding UTF8
Write-Host "  [SAVED]   $configPath" -ForegroundColor DarkCyan

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Banner "Generation Complete" -Color Green

Write-Host "  Project:  $($cfg.ProjectName)" -ForegroundColor White
Write-Host "  Domain:   $($cfg.ProjectDomain)" -ForegroundColor White
Write-Host "  Stack:    $lang$(if($fe -ne 'None'){" + $fe"})$(if($db -ne 'None'){" + $db"})" -ForegroundColor White
Write-Host "  Arch:     $arch$(if($useCqrs){" + CQRS"})" -ForegroundColor White
Write-Host "  Output:   $OutputPath" -ForegroundColor White
Write-Host ""

Write-Host "  Generated files:" -ForegroundColor Cyan
foreach ($f in $filesWritten) {
    Write-Host "    [+] $f" -ForegroundColor Green
}
Write-Host "    [+] .ai-infrastructure-config.json (reusable config)" -ForegroundColor DarkCyan
Write-Host ""

Write-Section "Next Steps"
Write-Host "  1. " -NoNewline -ForegroundColor Yellow; Write-Host "Review generated files and customize further for your project." -ForegroundColor White
Write-Host "  2. " -NoNewline -ForegroundColor Yellow; Write-Host "Copy .github/skills/ from the source project (if applicable)." -ForegroundColor White
Write-Host "  3. " -NoNewline -ForegroundColor Yellow; Write-Host "Copy .github/extensions/ and update tool configs." -ForegroundColor White
Write-Host "  4. " -NoNewline -ForegroundColor Yellow; Write-Host "Set up .claude/hooks/ for your build system (if using Claude)." -ForegroundColor White
Write-Host "  5. " -NoNewline -ForegroundColor Yellow; Write-Host "Test with your AI assistant to verify instructions work." -ForegroundColor White
Write-Host "  6. " -NoNewline -ForegroundColor Yellow; Write-Host "Re-run with -NonInteractive -ConfigFile .ai-infrastructure-config.json to regenerate." -ForegroundColor White
Write-Host ""
Write-Host "  Tip: " -NoNewline -ForegroundColor Magenta
Write-Host "Run with -Force to overwrite files when iterating on your instructions." -ForegroundColor Gray
Write-Host ""
