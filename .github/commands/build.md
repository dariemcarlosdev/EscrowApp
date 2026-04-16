---
description: Implement the next task incrementally — build one slice, test, verify, commit
---

Invoke incremental-implementation alongside tdd-coach. Read and follow:

```
cat .github/skills/code-quality/incremental-implementation/SKILL.md
cat .github/skills/testing/tdd-coach/SKILL.md
```

Pick the next pending task from the plan. For each task:

1. Read the task's acceptance criteria
2. Load relevant context — existing code, patterns, types
3. Write a failing test for expected behavior (RED)
4. Implement the minimum code to pass the test (GREEN)
5. Run `dotnet test` — verify no regressions
6. Run `dotnet build` — verify compilation
7. Commit with a descriptive conventional message
8. Mark the task complete and move to the next one

If any step fails, invoke the debugging-wizard skill:
```
cat .github/skills/code-quality/debugging-wizard/SKILL.md
```
