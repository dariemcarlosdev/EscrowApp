<#
.SYNOPSIS
    Exports the AI infrastructure starter kit from EscrowApp to a new project.
.DESCRIPTION
    Copies portable AI skills, extensions, rules, hooks, and generates
    customizable templates for project-specific instruction files.
.PARAMETER TargetPath
    Destination folder for the exported infrastructure.
.PARAMETER ProjectType
    "dotnet" (default) includes .NET-specific rules and extensions.
    "generic" copies only universal, language-agnostic assets.
.PARAMETER IncludeTemplates
    When set, generates placeholder AGENTS.md, CLAUDE.md, GEMINI.md,
    and copilot-instructions.md templates.
.PARAMETER Force
    Overwrite existing files in the target folder.
.EXAMPLE
    .\export-ai-infrastructure.ps1 -TargetPath "C:\Projects\MyNewApp" -IncludeTemplates
.EXAMPLE
    .\export-ai-infrastructure.ps1 -TargetPath "C:\Projects\NodeApp" -ProjectType "generic" -IncludeTemplates
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0, HelpMessage = "Destination folder for the exported infrastructure.")]
    [string]$TargetPath,

    [Parameter(Mandatory = $false)]
    [ValidateSet("dotnet", "generic")]
    [string]$ProjectType = "dotnet",

    [switch]$IncludeTemplates,

    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─────────────────────────────────────────────────────────────
# Resolve source root relative to script location
# Script lives at .github/scripts/ → source root is ../..
# ─────────────────────────────────────────────────────────────
$SourceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

# ─────────────────────────────────────────────────────────────
# Counters for summary report
# ─────────────────────────────────────────────────────────────
$script:FilesCopied = 0
$script:FilesSkipped = 0
$script:TemplatesGenerated = 0
$script:TotalBytesCopied = 0
$script:CategoryCounts = @{}

# ─────────────────────────────────────────────────────────────
# Helper: Write colored status messages
# ─────────────────────────────────────────────────────────────
function Write-Status {
    param(
        [string]$Message,
        [ValidateSet("Info", "Success", "Warning", "Error", "Header")]
        [string]$Level = "Info"
    )
    switch ($Level) {
        "Info"    { Write-Host "  $Message" -ForegroundColor Cyan }
        "Success" { Write-Host "  ✓ $Message" -ForegroundColor Green }
        "Warning" { Write-Host "  ⚠ $Message" -ForegroundColor Yellow }
        "Error"   { Write-Host "  ✗ $Message" -ForegroundColor Red }
        "Header"  { Write-Host "`n━━ $Message ━━" -ForegroundColor Magenta }
    }
}

# ─────────────────────────────────────────────────────────────
# Helper: Copy a single file with directory creation
# Returns $true if file was copied, $false if skipped
# ─────────────────────────────────────────────────────────────
function Copy-FileToTarget {
    param(
        [string]$SourceFile,
        [string]$DestFile,
        [string]$Category
    )

    if (-not (Test-Path $SourceFile)) {
        Write-Status "Source not found: $SourceFile" -Level Warning
        return $false
    }

    $destDir = Split-Path $DestFile -Parent
    if (-not (Test-Path $destDir)) {
        New-Item -Path $destDir -ItemType Directory -Force | Out-Null
    }

    if ((Test-Path $DestFile) -and -not $Force) {
        Write-Status "Skipped (exists): $DestFile" -Level Warning
        $script:FilesSkipped++
        return $false
    }

    Copy-Item -Path $SourceFile -Destination $DestFile -Force
    $fileSize = (Get-Item $DestFile).Length
    $script:TotalBytesCopied += $fileSize
    $script:FilesCopied++

    if (-not $script:CategoryCounts.ContainsKey($Category)) {
        $script:CategoryCounts[$Category] = 0
    }
    $script:CategoryCounts[$Category]++

    $relativeDest = $DestFile.Replace($TargetPath, "").TrimStart("\", "/")
    Write-Status "Copied: $relativeDest" -Level Success
    return $true
}

# ─────────────────────────────────────────────────────────────
# Helper: Recursively copy a directory
# ─────────────────────────────────────────────────────────────
function Copy-DirectoryToTarget {
    param(
        [string]$SourceDir,
        [string]$DestDir,
        [string]$Category
    )

    if (-not (Test-Path $SourceDir)) {
        Write-Status "Source directory not found: $SourceDir" -Level Warning
        return
    }

    $files = Get-ChildItem -Path $SourceDir -Recurse -File
    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring($SourceDir.Length).TrimStart("\", "/")
        $destFile = Join-Path $DestDir $relativePath
        Copy-FileToTarget -SourceFile $file.FullName -DestFile $destFile -Category $Category | Out-Null
    }
}

# ─────────────────────────────────────────────────────────────
# Helper: Write a template file
# ─────────────────────────────────────────────────────────────
function Write-TemplateFile {
    param(
        [string]$FilePath,
        [string]$Content,
        [string]$TemplateName
    )

    $destDir = Split-Path $FilePath -Parent
    if (-not (Test-Path $destDir)) {
        New-Item -Path $destDir -ItemType Directory -Force | Out-Null
    }

    if ((Test-Path $FilePath) -and -not $Force) {
        Write-Status "Skipped template (exists): $TemplateName" -Level Warning
        $script:FilesSkipped++
        return
    }

    Set-Content -Path $FilePath -Value $Content -Encoding UTF8
    $script:TemplatesGenerated++
    $script:TotalBytesCopied += (Get-Item $FilePath).Length
    Write-Status "Generated template: $TemplateName" -Level Success
}

# ─────────────────────────────────────────────────────────────
# Template content generators
# ─────────────────────────────────────────────────────────────
function Get-AgentsTemplate {
    return @'
# AGENTS.md — {PROJECT_NAME}

> Universal instructions for all AI coding agents working on this repository.

## Project Identity

<!-- TODO: Describe your project, tech stack, domain -->

**{PROJECT_NAME}** is a...

- **Domain:**
- **Users:**
- **Tech stack:**
- **Target:**

---

## Architecture Overview

<!-- TODO: Define your layer map and dependency direction -->

### Layer Map

```
Presentation    →  Components/           UI layer
Application     →  Features/             Use-case handlers
Domain          →  Models/               Entities, value objects, events
Infrastructure  →  Data/                 Data access, external services
```

### Dependency Rules — MANDATORY

- Inner layers NEVER reference outer layers.
- Domain must not depend on any infrastructure package.
- Infrastructure implements domain interfaces.

---

## Design Patterns

<!-- TODO: List patterns used and where they apply -->

| Pattern | Where | Purpose |
|---------|-------|---------|
| Repository | Data/ | Abstract data access behind interfaces |
| Strategy | Services/ | Swappable algorithm implementations |
| Mediator | Features/ | Decouple request handling |

---

## Code Conventions

- File-scoped namespaces
- Nullable reference types enabled
- `sealed` on classes not designed for inheritance
- `record` types for DTOs
- Async/await with CancellationToken propagation
- Guard clauses over nested conditionals
- No magic strings — use constants or enums
- Intention-revealing names — no abbreviations except well-known acronyms

---

## Security — OWASP Top 10

<!-- TODO: Customize for your domain -->

| Category | Requirement |
|----------|-------------|
| **Broken Access Control** | `[Authorize]` on every endpoint. Default deny. |
| **Cryptographic Failures** | Secrets via env vars or Key Vault. Never in source. |
| **Injection** | Parameterized queries only. No string concatenation. |
| **Insecure Design** | Threat model reviewed for business logic bypasses. |
| **Security Misconfiguration** | HTTPS enforced, HSTS enabled, antiforgery tokens. |
| **Vulnerable Components** | Keep packages updated. Monitor for CVEs. |
| **Auth Failures** | Validate tokens on every request. |
| **Logging Failures** | Structured logging with correlation IDs. Never log secrets or PII. |

---

## Documentation

<!-- TODO: Map your docs/ structure -->

Update `docs/` when features change. Organize by concern:

```
docs/
├── architecture/     ← system design, patterns
├── features/         ← feature-specific documentation
├── cross-cutting/    ← auth, logging, localization
└── operations/       ← deployment, monitoring
```

---

## Skills Catalog

See `.github/skills/CATALOG.md` for the complete skill catalog.

### How to Use a Skill (Any Model)

```bash
# Step 1: Find the right skill
cat .github/skills/CATALOG.md

# Step 2: Read the skill core file
cat .github/skills/{category}/{skill-name}/SKILL.md

# Step 3: Follow the Core Workflow inside

# Step 4: Load references on demand from the Reference Guide table
cat .github/skills/{category}/{skill-name}/references/{topic}.md
```
'@
}

function Get-ClaudeTemplate {
    return @'
# CLAUDE.md — Claude-Specific Instructions

> Read **AGENTS.md** first for full project context. This file adds Claude-specific guidance.

## Project Context

<!-- TODO: Brief project summary for Claude's reasoning capabilities -->

---

## Reasoning Approach

### Architectural Decisions

When making architectural decisions, reason through this checklist:

1. **Which layer does this belong to?** Map the change to Presentation / Application / Domain / Infrastructure.
2. **Does it violate dependency direction?** Inner layers must never reference outer layers.
3. **Which pattern applies?** Match to the patterns listed in AGENTS.md.
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
3. **Evaluate SOLID impact.** Which principles are violated?
4. **Plan the migration.** Backward compatibility matters.
5. **Verify invariants.** After refactoring, do business rules still hold?

---

## Code Generation Rules

<!-- TODO: Add language-specific code generation rules -->

### General

- File-scoped namespaces
- Nullable reference types enabled
- `sealed` on classes not designed for inheritance
- `record` types for DTOs and immutable data
- Primary constructors for simple DI injection
- Cancellation tokens on every async method

### Blazor Components (if applicable)

Always generate three files per component:

```
ComponentName.razor       ← Markup only. No @code {} blocks.
ComponentName.razor.cs    ← sealed partial class. All logic here.
ComponentName.razor.css   ← Scoped CSS.
```

---

## Security Review Methodology

When reviewing code for security, systematically evaluate each OWASP Top 10 category:

| # | Category | What to Check |
|---|----------|---------------|
| A01 | Broken Access Control | `[Authorize]` on every endpoint? Policy-based? |
| A02 | Cryptographic Failures | Secrets in code? PII in logs? TLS enforced? |
| A03 | Injection | Parameterized queries? No string concatenation? |
| A04 | Insecure Design | Threat model reviewed? Business logic bypasses? |
| A05 | Security Misconfiguration | HTTPS? HSTS? Antiforgery? Debug disabled in prod? |
| A06 | Vulnerable Components | Packages up to date? Known CVEs? |
| A07 | Auth Failures | Token validation? Brute-force protection? |
| A08 | Data Integrity Failures | Deserialization safe? |
| A09 | Logging Failures | Audit trail? Correlation IDs? No secrets in logs? |
| A10 | SSRF | External URL validation? Allowlisting? |

For each finding, provide:
- **Severity:** Critical / High / Medium / Low
- **Location:** File and line reference
- **Issue:** What's wrong
- **Fix:** Specific code change

---

## Documentation Updates

<!-- TODO: Map features to their documentation files -->

| Feature Area | Doc to Update |
|--------------|---------------|
| Core feature | `docs/features/{feature-name}` |
| Architecture | `docs/architecture/overview` |
| Security | `docs/audits/security-audit` |

---

## Skills Catalog

See **AGENTS.md → Skills Catalog** for complete instructions.
All skills in `.github/skills/` are registered as Claude Code skills in `.claude/skills/`.
'@
}

function Get-GeminiTemplate {
    return @'
# GEMINI.md — Gemini-Specific Instructions

> Read **AGENTS.md** first for full project context. This file adds Gemini-specific guidance.

## Project Context

<!-- TODO: Brief project summary for Gemini's analysis capabilities -->

---

## Exploration Strategy

### Before Making Changes — Map Dependencies First

When asked to modify any code, analyze the dependency graph before writing:

1. **Trace inbound references.** What calls/imports the file being changed?
2. **Trace outbound references.** What does the file depend on?
3. **Identify the layer.** Presentation → Application → Domain ← Infrastructure.
4. **Check for pattern consistency.** How do similar files in the same directory handle this?
5. **Verify interface contracts.** If changing an interface, identify all implementations and consumers.

### Cross-Referencing Checklist

<!-- TODO: Fill in project-specific locations -->

| Question | Where to Look |
|----------|---------------|
| How is DI wired? | `Program.cs` |
| What patterns exist? | `Services/` |
| What handlers exist? | `Features/` |
| What domain events exist? | `Events/` |
| What's the DB schema? | `Models/`, `Data/` |

---

## Code Generation Guidelines

### Match Existing Patterns

Before generating code, find and match the project's established patterns:

<!-- TODO: Add your project's specific patterns as references -->

### Code Style Rules

- File-scoped namespaces
- Nullable reference types enabled
- `sealed` on concrete classes not designed for inheritance
- `record` types for commands, queries, and DTOs
- Async/await with `CancellationToken` propagation
- Guard clauses at method entry — fail fast
- Explicit types for domain types; `var` for obvious types

---

## Database Guidance

<!-- TODO: Customize for your ORM and database -->

### Before Writing Queries

1. Check existing repository methods.
2. Examine DbContext for configured relationships and indexes.
3. Match existing query patterns.
4. Check for existing migrations before creating new ones.

### Query Rules

- Always use parameterized queries — never raw SQL string concatenation.
- Read queries: `AsNoTracking()` for performance.
- Writes: load entity → modify → `SaveChangesAsync()`.

---

## Feature Modification Workflow

When adding or modifying a feature:

```
1. Identify the vertical slice or module
2. Check the corresponding doc in docs/
3. Map dependencies (repository, services, events)
4. Make changes following existing patterns
5. Update the docs/ entry
6. Verify DI registration in Program.cs if new services added
7. Add/update localization keys if UI text changes
```

---

## UI Component Analysis

<!-- TODO: Customize for your UI framework -->

When working with UI components:

1. Inspect all related files for the component.
2. Check parent-child relationships and data flow.
3. Verify localization of user-facing strings.
4. Check scoped styles.
5. Match existing component patterns for consistency.

---

## Documentation Maintenance

<!-- TODO: Map your docs/ structure -->

When features change, update the corresponding doc:

```
docs/
├── architecture/     ← cross-cutting changes
├── features/         ← feature-specific changes
├── cross-cutting/    ← auth, logging, localization
└── operations/       ← deployment, monitoring
```

New features that don't fit existing docs: create one under the appropriate concern category.

---

## Skills Catalog

See **AGENTS.md → Skills Catalog** for the complete skill loading instructions.
Skills are universal across all models — read them with file tools and follow the Core Workflow.
'@
}

function Get-CopilotInstructionsTemplate {
    return @'
# Copilot Instructions — {PROJECT_NAME}

> Master project-level instructions for GitHub Copilot and all AI coding assistants.

## Developer Profile

- **Role:** Senior Developer
- **Expertise:** <!-- TODO: List your expertise areas -->
- **Mindset:** SOLID principles, Clean Code, security-in-depth

## General Preferences

- **Language:** Respond in English
- **Tone:** Concise, professional, technically precise
- **Code comments:** Only when logic needs clarification
- **Error handling:** Always include proper error handling
- **Async:** Prefer async/await patterns
- **Naming:** Intention-revealing names; no abbreviations except well-known acronyms

## Architecture Principles

<!-- TODO: Customize for your architecture -->

- **Clean Architecture:** Enforce dependency inversion — outer layers depend on inner
- **SOLID Principles:** Apply consistently across the codebase
- **Repository Pattern:** Abstract data access behind interfaces

## Security (OWASP Top 10 Mindset)

- **Injection:** Always parameterize queries
- **Broken Auth:** Never store plaintext passwords
- **Sensitive Data:** Never log PII, tokens, or connection strings
- **XSS:** Sanitize and encode all output
- **Access Control:** Validate authorization on every endpoint

## Code Review Guidance

When reviewing or generating code, evaluate against:

- **Correctness:** Does it handle edge cases and null inputs?
- **SOLID compliance:** Single responsibility? Depending on abstractions?
- **Clean Code:** Readable names? Small methods? No duplication?
- **Error handling:** Are exceptions meaningful?
- **Security:** Input validated? Authorization checked?
- **Performance:** Unnecessary allocations? Missing CancellationToken?
- **Testability:** Can this be unit-tested without infrastructure?
'@
}

# ═════════════════════════════════════════════════════════════
# MAIN EXECUTION
# ═════════════════════════════════════════════════════════════

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     AI Infrastructure Export — Starter Kit Generator    ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Source:       $SourceRoot" -ForegroundColor DarkGray
Write-Host "  Target:       $TargetPath" -ForegroundColor DarkGray
Write-Host "  Project Type: $ProjectType" -ForegroundColor DarkGray
Write-Host "  Templates:    $($IncludeTemplates.IsPresent)" -ForegroundColor DarkGray
Write-Host "  Force:        $($Force.IsPresent)" -ForegroundColor DarkGray

# ─────────────────────────────────────────────────────────────
# Validate source root
# ─────────────────────────────────────────────────────────────
if (-not (Test-Path $SourceRoot)) {
    Write-Status "Source root not found: $SourceRoot" -Level Error
    Write-Status "Run this script from its original location inside .github/scripts/" -Level Error
    exit 1
}

$requiredSourceDirs = @(
    (Join-Path $SourceRoot ".github\skills"),
    (Join-Path $SourceRoot ".claude\skills"),
    (Join-Path $SourceRoot ".github\extensions\superpowers")
)

foreach ($dir in $requiredSourceDirs) {
    if (-not (Test-Path $dir)) {
        Write-Status "Required source directory not found: $dir" -Level Error
        exit 1
    }
}

# ─────────────────────────────────────────────────────────────
# Create target directory
# ─────────────────────────────────────────────────────────────
if (-not (Test-Path $TargetPath)) {
    New-Item -Path $TargetPath -ItemType Directory -Force | Out-Null
    Write-Status "Created target directory: $TargetPath" -Level Info
}

$TargetPath = (Resolve-Path $TargetPath).Path

# ═════════════════════════════════════════════════════════════
# PHASE 1: Copy 100% portable files
# ═════════════════════════════════════════════════════════════
Write-Status "PORTABLE FILES (universal, language-agnostic)" -Level Header

# 1a. .github/skills/ entire directory
Write-Status "Copying .github/skills/ (AI skill definitions)..." -Level Info
Copy-DirectoryToTarget `
    -SourceDir (Join-Path $SourceRoot ".github\skills") `
    -DestDir   (Join-Path $TargetPath ".github\skills") `
    -Category  "GitHub Skills"

# 1b. .claude/skills/ entire directory
Write-Status "Copying .claude/skills/ (Claude bridge files)..." -Level Info
Copy-DirectoryToTarget `
    -SourceDir (Join-Path $SourceRoot ".claude\skills") `
    -DestDir   (Join-Path $TargetPath ".claude\skills") `
    -Category  "Claude Skills"

# 1c. .github/extensions/superpowers/
Write-Status "Copying .github/extensions/superpowers/..." -Level Info
Copy-DirectoryToTarget `
    -SourceDir (Join-Path $SourceRoot ".github\extensions\superpowers") `
    -DestDir   (Join-Path $TargetPath ".github\extensions\superpowers") `
    -Category  "Extensions"

# 1d. Portable Claude rules
$portableRules = @(
    "clean-architecture.md",
    "mvp-first.md",
    "memory-optimization.md"
)

Write-Status "Copying portable Claude rules..." -Level Info
foreach ($rule in $portableRules) {
    Copy-FileToTarget `
        -SourceFile (Join-Path $SourceRoot ".claude\rules\$rule") `
        -DestFile   (Join-Path $TargetPath ".claude\rules\$rule") `
        -Category   "Claude Rules (portable)" | Out-Null
}

# 1e. .claude/settings.local.json
Copy-FileToTarget `
    -SourceFile (Join-Path $SourceRoot ".claude\settings.local.json") `
    -DestFile   (Join-Path $TargetPath ".claude\settings.local.json") `
    -Category   "Claude Config" | Out-Null

# ═════════════════════════════════════════════════════════════
# PHASE 2: .NET-specific files (if ProjectType = "dotnet")
# ═════════════════════════════════════════════════════════════
if ($ProjectType -eq "dotnet") {
    Write-Status ".NET-SPECIFIC FILES" -Level Header

    # .NET Claude rules
    $dotnetRules = @(
        "cqrs-mediatr.md",
        "blazor-components.md",
        "owasp-security.md",
        "polly-resilience.md",
        "testing-standards.md"
    )

    Write-Status "Copying .NET-specific Claude rules..." -Level Info
    foreach ($rule in $dotnetRules) {
        Copy-FileToTarget `
            -SourceFile (Join-Path $SourceRoot ".claude\rules\$rule") `
            -DestFile   (Join-Path $TargetPath ".claude\rules\$rule") `
            -Category   "Claude Rules (.NET)" | Out-Null
    }

    # ef-core.md with TODO comment prepended
    $efCoreSrc = Join-Path $SourceRoot ".claude\rules\ef-core.md"
    $efCoreDst = Join-Path $TargetPath ".claude\rules\ef-core.md"

    if (Test-Path $efCoreSrc) {
        if ((Test-Path $efCoreDst) -and -not $Force) {
            Write-Status "Skipped (exists): .claude\rules\ef-core.md" -Level Warning
            $script:FilesSkipped++
        }
        else {
            $efCoreDir = Split-Path $efCoreDst -Parent
            if (-not (Test-Path $efCoreDir)) {
                New-Item -Path $efCoreDir -ItemType Directory -Force | Out-Null
            }
            $todoComment = "<!-- TODO: Customize EF Core patterns for your database provider (PostgreSQL, SQL Server, SQLite) and entity configuration. -->`n"
            $originalContent = Get-Content $efCoreSrc -Raw
            Set-Content -Path $efCoreDst -Value ($todoComment + $originalContent) -Encoding UTF8
            $fileSize = (Get-Item $efCoreDst).Length
            $script:TotalBytesCopied += $fileSize
            $script:FilesCopied++
            if (-not $script:CategoryCounts.ContainsKey("Claude Rules (.NET)")) {
                $script:CategoryCounts["Claude Rules (.NET)"] = 0
            }
            $script:CategoryCounts["Claude Rules (.NET)"]++
            Write-Status "Copied (with TODO): .claude\rules\ef-core.md" -Level Success
        }
    }
    else {
        Write-Status "Source not found: $efCoreSrc" -Level Warning
    }

    # .NET extensions
    $dotnetExtensions = @("dotnet-conventions", "build-guardian")
    foreach ($ext in $dotnetExtensions) {
        $extSrc = Join-Path $SourceRoot ".github\extensions\$ext"
        $extDst = Join-Path $TargetPath ".github\extensions\$ext"
        if (Test-Path $extSrc) {
            Write-Status "Copying .github/extensions/$ext/..." -Level Info
            Copy-DirectoryToTarget -SourceDir $extSrc -DestDir $extDst -Category "Extensions (.NET)"
        }
        else {
            Write-Status "Extension not found: $ext" -Level Warning
        }
    }
}
else {
    Write-Status ".NET FILES SKIPPED (ProjectType = generic)" -Level Header
    Write-Status "Use -ProjectType dotnet to include .NET rules and extensions." -Level Info
}

# ═════════════════════════════════════════════════════════════
# PHASE 3: Generate templates (if -IncludeTemplates)
# ═════════════════════════════════════════════════════════════
if ($IncludeTemplates) {
    Write-Status "TEMPLATE GENERATION" -Level Header

    Write-TemplateFile `
        -FilePath     (Join-Path $TargetPath "AGENTS.md") `
        -Content      (Get-AgentsTemplate) `
        -TemplateName "AGENTS.md"

    Write-TemplateFile `
        -FilePath     (Join-Path $TargetPath "CLAUDE.md") `
        -Content      (Get-ClaudeTemplate) `
        -TemplateName "CLAUDE.md"

    Write-TemplateFile `
        -FilePath     (Join-Path $TargetPath "GEMINI.md") `
        -Content      (Get-GeminiTemplate) `
        -TemplateName "GEMINI.md"

    Write-TemplateFile `
        -FilePath     (Join-Path $TargetPath ".github\copilot-instructions.md") `
        -Content      (Get-CopilotInstructionsTemplate) `
        -TemplateName ".github/copilot-instructions.md"
}

# ═════════════════════════════════════════════════════════════
# SUMMARY REPORT
# ═════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║                    EXPORT COMPLETE                      ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

# Files by category
Write-Host "  Files copied by category:" -ForegroundColor White
foreach ($category in $script:CategoryCounts.Keys | Sort-Object) {
    $count = $script:CategoryCounts[$category]
    Write-Host "    $category" -ForegroundColor Cyan -NoNewline
    Write-Host (" " * [Math]::Max(1, 35 - $category.Length)) -NoNewline
    Write-Host "$count files" -ForegroundColor White
}
Write-Host ""

# Summary numbers
$sizeKB = [Math]::Round($script:TotalBytesCopied / 1KB, 1)
$sizeMB = [Math]::Round($script:TotalBytesCopied / 1MB, 2)
$sizeDisplay = if ($sizeMB -ge 1) { "${sizeMB} MB" } else { "${sizeKB} KB" }

Write-Host "  ┌─────────────────────────────────────┐" -ForegroundColor DarkGray
Write-Host "  │ Files copied:        " -ForegroundColor DarkGray -NoNewline
Write-Host ("{0,-15}" -f $script:FilesCopied) -ForegroundColor White -NoNewline
Write-Host "│" -ForegroundColor DarkGray
Write-Host "  │ Files skipped:       " -ForegroundColor DarkGray -NoNewline
Write-Host ("{0,-15}" -f $script:FilesSkipped) -ForegroundColor Yellow -NoNewline
Write-Host "│" -ForegroundColor DarkGray
Write-Host "  │ Templates generated: " -ForegroundColor DarkGray -NoNewline
Write-Host ("{0,-15}" -f $script:TemplatesGenerated) -ForegroundColor White -NoNewline
Write-Host "│" -ForegroundColor DarkGray
Write-Host "  │ Total size:          " -ForegroundColor DarkGray -NoNewline
Write-Host ("{0,-15}" -f $sizeDisplay) -ForegroundColor White -NoNewline
Write-Host "│" -ForegroundColor DarkGray
Write-Host "  └─────────────────────────────────────┘" -ForegroundColor DarkGray
Write-Host ""

# Next steps
Write-Host "  Next steps:" -ForegroundColor Yellow
Write-Host "  ─────────────────────────────────────" -ForegroundColor DarkGray

$stepNum = 1

if ($IncludeTemplates) {
    Write-Host "  $stepNum. Edit AGENTS.md — replace {PROJECT_NAME} and fill in TODO sections" -ForegroundColor White
    $stepNum++
    Write-Host "  $stepNum. Edit CLAUDE.md — add project-specific reasoning guidance" -ForegroundColor White
    $stepNum++
    Write-Host "  $stepNum. Edit GEMINI.md — add cross-referencing locations" -ForegroundColor White
    $stepNum++
    Write-Host "  $stepNum. Edit .github/copilot-instructions.md — add applyTo rules" -ForegroundColor White
    $stepNum++
}

Write-Host "  $stepNum. Review .github/skills/CATALOG.md for available skills" -ForegroundColor White
$stepNum++
Write-Host "  $stepNum. Run ``git add .github/ .claude/`` to track AI infrastructure" -ForegroundColor White
$stepNum++

if ($IncludeTemplates) {
    Write-Host "  $stepNum. Run ``git add AGENTS.md CLAUDE.md GEMINI.md`` to track instruction files" -ForegroundColor White
    $stepNum++
}

Write-Host "  $stepNum. Customize .claude/rules/ for your project's specific conventions" -ForegroundColor White
Write-Host ""
