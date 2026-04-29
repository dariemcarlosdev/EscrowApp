---
description: Run OWASP Top 10 security review on changed files
---

1. Identify changed files
   git diff --name-only HEAD~1 // turbo

2. For each .cs file, check:
   - A01: `[Authorize]` on every endpoint/page
   - A03: No string concatenation in SQL queries
   - A02: No secrets in source code
   - A07: API key validation on every request

3. For each .razor file, check:
   - No `@((MarkupString)untrustedContent)` XSS vectors
   - All strings use `@L["Key"]` localization

4. Run secret scan
   - Check for hardcoded API keys, connection strings, passwords

5. Verify HTTPS enforcement in Program.cs

6. Document findings with severity ratings (Critical/High/Medium/Low)
