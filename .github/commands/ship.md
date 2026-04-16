---
description: Pre-deployment checks — verify build, tests, security, and readiness before shipping
---

Invoke the deployment-preflight skill. Read and follow:

```
cat .github/skills/devops/deployment-preflight/SKILL.md
```

Run the pre-ship checklist:

1. **Build** — `dotnet build` passes with zero warnings
2. **Tests** — `dotnet test` — all tests pass
3. **Security** — No hardcoded secrets (`/security` audit)
4. **Auth** — Every endpoint has `[Authorize]`
5. **Docs** — Feature docs updated in `docs/`
6. **Planning** — `docs/planning/` updated with current status
7. **Migrations** — Database migrations reviewed and tested
8. **Config** — Environment-specific settings verified
9. **Compliance** — No "escrow" in user-facing copy (regulatory requirement)
10. **Changelog** — Version bumped, changes documented

Generate a go/no-go report with evidence for each checkpoint.

⚠️ Compliance-sensitive — requires legal review before production deployment.
