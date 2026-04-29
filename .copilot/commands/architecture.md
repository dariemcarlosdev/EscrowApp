---
description: Review system architecture for quality attributes, anti-patterns, and SOLID compliance
---

Invoke the architecture-reviewer skill. Read and follow:

```
cat .github/skills/architecture/architecture-reviewer/SKILL.md
```

Analyze the architecture against:

1. **Layer boundaries** — Do dependencies point inward? (Components → Features → Models ← Data)
2. **SOLID compliance** — SRP, OCP, LSP, ISP, DIP violations?
3. **Pattern usage** — Strategy, Repository, Factory, Event Bus applied correctly?
4. **DDD integrity** — Aggregate boundaries respected? Domain events after persistence?
5. **Clean Architecture** — Domain free of framework dependencies?
6. **Security posture** — OWASP Top 10 addressed? Default deny?

For design pattern recommendations:
```
cat .github/skills/architecture/design-pattern-advisor/SKILL.md
```

For dependency analysis:
```
cat .github/skills/architecture/dependency-analyzer/SKILL.md
```

Report findings as: Quality Attribute | Current State | Risk Level | Recommendation
