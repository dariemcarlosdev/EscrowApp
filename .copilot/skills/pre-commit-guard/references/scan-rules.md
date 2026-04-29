# Pre-Commit Security Guard — Scan Rules Reference

> Load this file when you need detailed pattern definitions, regex, false positive guidance, or remediation steps.

---

## CRITICAL Rules — Commit Blockers

These findings **block commits** via the git hook. They represent secrets in source code that, if pushed to a remote, are immediately exploitable.

### CRIT-001: Stripe Secret Key

| Field | Value |
|-------|-------|
| **Pattern** | `sk_(live\|test)_[a-zA-Z0-9]{20,}` |
| **Risk** | Stripe secret keys grant full API access — create charges, refunds, read customer data |
| **False positives** | Pattern definitions in security tooling (this file, extension.mjs, hooks) |
| **Remediation** | Move to environment variable `STRIPE_SECRET_KEY`. Use `IOptions<StripeSettings>` in code. For dev: `dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."`. For prod: Azure Key Vault. |

### CRIT-002: GitHub Token

| Field | Value |
|-------|-------|
| **Pattern** | `(ghp\|gho\|ghu\|ghs\|ghr)_[a-zA-Z0-9]{36,}` |
| **Risk** | GitHub tokens grant repository access — read/write code, issues, actions |
| **Remediation** | Use `GITHUB_TOKEN` environment variable. For Actions: `${{ secrets.GITHUB_TOKEN }}`. |

### CRIT-003: AWS Secret Key

| Field | Value |
|-------|-------|
| **Pattern** | `(aws_secret_access_key\|AKIA)[a-zA-Z0-9/+=]{20,}` (case insensitive) |
| **Risk** | AWS keys grant cloud infrastructure access — S3, EC2, IAM |
| **Remediation** | Use AWS IAM roles, instance profiles, or `AWS_SECRET_ACCESS_KEY` env var. |

### CRIT-004: Hardcoded Password

| Field | Value |
|-------|-------|
| **Pattern** | `(password\|passwd\|pwd)\s*[:=]\s*["'][^"']{4,}["']` (case insensitive) |
| **Risk** | Plaintext passwords in source code — often database or service credentials |
| **False positives** | Test fixtures with dummy passwords (e.g., `"TestPassword123!"`) — review manually |
| **Remediation** | Use `dotnet user-secrets` for dev, Azure Key Vault for prod. Never commit real passwords. |

### CRIT-005: Connection String Password

| Field | Value |
|-------|-------|
| **Pattern** | `(Password\|PWD)\s*=\s*[^;"\s]{4,}` (case insensitive) |
| **Risk** | Database credentials in connection strings — direct database access |
| **False positives** | Placeholder values like `${DB_PASSWORD}` or `__PASSWORD__` |
| **Remediation** | Use `ConnectionStrings__DefaultConnection` env var or Key Vault reference. |

### CRIT-006: Private Key

| Field | Value |
|-------|-------|
| **Pattern** | `-----BEGIN\s+(RSA\s+)?PRIVATE\s+KEY-----` |
| **Risk** | Private keys enable impersonation, decryption, signing |
| **Remediation** | Store in Key Vault, certificate store, or HSM. Never in source. |

---

## HIGH Rules — Should Fix Before Commit

These don't block the git hook but are flagged by the extension's full scan. They represent significant security gaps.

### HIGH-001: Generic API Key

| Field | Value |
|-------|-------|
| **Pattern** | `(api[_-]?key\|apikey)\s*[:=]\s*["'][a-zA-Z0-9_\-]{16,}["']` |
| **Risk** | API keys in source may grant access to third-party services |
| **Remediation** | Move to configuration. Use `IOptions<T>` pattern. |

### HIGH-002: Bearer Token Hardcoded

| Field | Value |
|-------|-------|
| **Pattern** | `["']Bearer\s+[a-zA-Z0-9._\-]{20,}["']` |
| **Risk** | Hardcoded auth tokens bypass authentication — anyone with the code has access |
| **Remediation** | Use token acquisition at runtime via auth flows (MSAL, OIDC). |

### HIGH-003: JWT Token in Source

| Field | Value |
|-------|-------|
| **Pattern** | `eyJ[a-zA-Z0-9_-]{10,}\.eyJ[a-zA-Z0-9_-]{10,}\.[a-zA-Z0-9_\-]{10,}` |
| **Risk** | JWTs contain claims and may grant access if not expired |
| **False positives** | Test fixture tokens — verify they use expired/test signing keys |
| **Remediation** | Remove from source. Generate tokens at runtime. |

### HIGH-004: Missing [Authorize]

| Field | Value |
|-------|-------|
| **Check** | Structural — `.razor` files with `@page` but no `@attribute [Authorize]` or `[AllowAnonymous]` |
| **Check** | Structural — `.cs` files with `[ApiController]` but no `[Authorize]` |
| **Risk** | Unauthenticated access to protected resources (OWASP A01) |
| **Remediation** | Add `@attribute [Authorize]` to Blazor pages, `[Authorize]` to controllers. Use `[AllowAnonymous]` only for genuinely public endpoints (login, landing page). |

### HIGH-005: SQL Injection Risk

| Field | Value |
|-------|-------|
| **Pattern** | `(FromSqlRaw\|ExecuteSqlRaw)\s*\(\s*\$"` |
| **Risk** | String interpolation in raw SQL enables SQL injection (OWASP A03) |
| **Remediation** | Replace with `FromSqlInterpolated` (parameterizes automatically) or use LINQ. |

### HIGH-006: Crypto Key Material

| Field | Value |
|-------|-------|
| **Pattern** | `(secret\|signing[_-]?key)\s*[:=]\s*["'][a-zA-Z0-9+/=]{16,}["']` |
| **Risk** | Signing keys in source enable token forgery |
| **Remediation** | Store in Key Vault. Use `IDataProtectionProvider` for app-level crypto. |

### HIGH-007: Webhook Secret

| Field | Value |
|-------|-------|
| **Pattern** | `whsec_[a-zA-Z0-9]{20,}` |
| **Risk** | Stripe webhook secrets allow forging webhook events |
| **Remediation** | Move to env var `STRIPE_WEBHOOK_SECRET`. |

---

## MEDIUM Rules — Fix Before Merge

### MED-001: TODO Security Comment

| Field | Value |
|-------|-------|
| **Pattern** | `TODO.*(?:security\|auth\|encrypt\|secret\|password)` |
| **Risk** | Security-related TODOs indicate known gaps that haven't been addressed |
| **Remediation** | Resolve the TODO or create a tracked issue before merging. |

### MED-002: Console.Write in Production

| Field | Value |
|-------|-------|
| **Pattern** | `Console\.Write(?:Line)?\s*\(` |
| **Risk** | Console output may leak sensitive data and bypasses structured logging |
| **Remediation** | Replace with `ILogger<T>`. Never log PII or secrets. |

### MED-003: Disabled SSL Validation

| Field | Value |
|-------|-------|
| **Pattern** | `ServerCertificateCustomValidationCallback\s*=.*=>.*true` |
| **Risk** | Disabling SSL validation enables man-in-the-middle attacks |
| **Remediation** | Remove the callback. Use proper certificates. For dev: use `dotnet dev-certs`. |

---

## Exclusion Rules

The scanner skips findings in these contexts to avoid false positives:

| Context | Why Excluded |
|---------|-------------|
| Lines starting with `//`, `*`, `<!--` | Comments documenting patterns, not real usage |
| Files in `.github/extensions/pre-commit-guard/` | Pattern definitions in the scanner itself |
| Files in `.github/extensions/security-scanner/` | Pattern definitions in the security scanner |
| Files in `.github/skills/` | Security documentation and examples |
| Files in `.github/hooks/` | Hook pattern definitions |
| Files in `bin/`, `obj/`, `node_modules/` | Build artifacts, not source |

---

## Platform-Specific Notes

### Copilot CLI
- Extension auto-detects 16+ trigger patterns (English + Spanish)
- `onPreToolUse` hook intercepts `git commit`/`git push` commands
- Tools: `pre_commit_security_scan`, `install_pre_commit_hook`

### Claude Code
- Bridge at `.claude/skills/pre-commit-guard/SKILL.md`
- Can invoke via `/pre-commit-guard` in Claude's skill system
- Reads this universal skill + applies patterns manually

### Codex CLI
- Referenced in `CODEX.md` → Security Checklist section
- Reads `.github/skills/security/pre-commit-guard/SKILL.md` directly
- Applies patterns as part of autonomous pre-commit checks

### Gemini
- Referenced in `GEMINI.md` → Security section
- Reads `.github/skills/security/pre-commit-guard/SKILL.md` directly
- Applies patterns during code generation and review

### Git Hook (all platforms)
- Shell script at `.github/hooks/pre-commit`
- Install to `.git/hooks/pre-commit`
- CRITICAL-only gate — works without any AI model
- Compatible: Git Bash (Windows), Bash (macOS/Linux), sh (CI)
