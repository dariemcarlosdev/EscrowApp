---
description: Break a feature or spec into verifiable tasks with dependencies and acceptance criteria
---

Invoke the feature-forge skill. Read and follow:

```
cat .github/skills/project-management/feature-forge/SKILL.md
```

For the current feature or spec:

1. Identify all components and their dependencies
2. Break into discrete tasks — each completable in a single session
3. Each task must have: acceptance criteria, verification step, files involved
4. Order tasks by dependency, not importance
5. No task should touch more than ~5 files
6. Output a structured task list ready for incremental-implementation

If no spec exists yet, use `/spec` first.
