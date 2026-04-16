---
description: Analyze code and execute metrics-driven refactoring with before/after comparison
---

Invoke the smart-refactor skill. Read and follow:

```
cat .github/skills/code-quality/smart-refactor/SKILL.md
```

For the target code:

1. **Baseline** — Measure current metrics (complexity, coupling, duplication)
2. **Identify smells** — Name specific code smells (God class, feature envy, long method, etc.)
3. **Plan** — Choose refactoring technique (Extract Method, Strategy, Decorator, etc.)
4. **Execute** — Apply refactoring in small, testable steps
5. **Verify** — Run tests after each step, compare metrics
6. **Document** — Record what changed and why

For planning-only analysis, use refactor-planner instead:
```
cat .github/skills/code-quality/refactor-planner/SKILL.md
```

For tech debt assessment:
```
cat .github/skills/code-quality/tech-debt-tracker/SKILL.md
```
