---
description: Audit code against OWASP Top 10 and scan for hardcoded secrets
---

Invoke owasp-audit and secret-scanner skills. Read and follow:

```
cat .github/skills/security/owasp-audit/SKILL.md
cat .github/skills/security/secret-scanner/SKILL.md
```

Perform a comprehensive security audit:

1. **OWASP Top 10 scan** — Check each category (A01-A10) against the codebase
2. **Secret scan** — Find hardcoded credentials, API keys, connection strings
3. **Auth review** — Verify [Authorize] on every endpoint, policy-based auth
4. **Input validation** — Confirm FluentValidation on every command
5. **Injection check** — Verify parameterized queries, no SQL concatenation

For each finding, provide:
- **Severity:** Critical / High / Medium / Low
- **Location:** File and line
- **Issue:** What's wrong
- **Fix:** Specific code change

For threat modeling, also invoke:
```
cat .github/skills/security/threat-modeler/SKILL.md
```

⚠️ This is a fintech platform — security findings are never deferred.
