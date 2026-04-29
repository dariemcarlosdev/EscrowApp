---
description: Systematic debugging with root cause analysis — reproduce, localize, fix, guard
---

Invoke the debugging-wizard skill. Read and follow:

```
cat .github/skills/code-quality/debugging-wizard/SKILL.md
```

Follow the systematic debugging process:

1. **Reproduce** — Get a reliable reproduction of the failure
2. **Localize** — Narrow down to the specific file, method, and line
3. **Diagnose** — Understand the root cause (not just the symptom)
4. **Fix** — Apply the minimum change that fixes the root cause
5. **Guard** — Write a regression test that fails without the fix
6. **Verify** — Run full test suite to confirm no side effects

**Do NOT:**
- Apply a fix without understanding the root cause
- Skip the regression test
- Make unrelated changes while debugging
- Assume the first hypothesis is correct — verify with evidence

If the bug involves payment flows, also check idempotency and domain event ordering.
