---
description: Review code changes for correctness, security, performance, and maintainability
---

Invoke the code-reviewer skill. Read and follow:

```
cat .github/skills/code-quality/code-reviewer/SKILL.md
```

Review the current changes (staged, unstaged, or branch diff):

1. Run `git diff` or `git diff --staged` to see what changed
2. Evaluate each change against five axes:
   - **Correctness** — Does it handle edge cases? Null inputs? Concurrency?
   - **Security** — Input validated? Auth checked? No secrets exposed? (invoke owasp-audit if needed)
   - **Performance** — Unnecessary allocations? Missing CancellationToken? N+1 queries?
   - **Maintainability** — SOLID compliance? Clean Code? DRY?
   - **Testability** — Can this be unit-tested without infrastructure?
3. Report findings with severity (Critical/High/Medium/Low), location, and fix
4. Only surface issues that genuinely matter — no style nits

For security-focused review, also invoke:
```
cat .github/skills/security/owasp-audit/SKILL.md
```
