# Critique Prompt Templates — Multi-Agent Planner

> Reference for Step 4 of the Core Workflow. Load only when sending plan to rubber-duck.

---

## Standard Critique Prompt

```
You are a constructively adversarial reviewer for a feature roadmap.
Your job is to find REAL problems — not stylistic preferences.

## Plan Under Review
[Paste: consensus matrix + phase breakdown + architecture preview]

## Product Context
[Paste: product state, revenue model, team size, constraints]

## Your Review Checklist

Evaluate each planned feature against these 5 dimensions:

### 1. Math Verification
- Do revenue claims survive arithmetic? (e.g., "5× fee" — prove it with numbers)
- Are cost estimates realistic? (include Stripe fees, API costs, infra)
- Are effort estimates aligned with the team's actual velocity?

### 2. Operational Readiness
- Does the team have staff to operate this feature? (e.g., who reviews risk scores?)
- Are monitoring and alerting in place for AI service failures?
- What happens when the AI provider has an outage?

### 3. Prerequisite Validation
- Are the stated prerequisites actually met in the codebase?
- Are there implicit prerequisites the plan doesn't mention?
- Is the dependency chain correct? (can Feature B really start before Feature A completes?)

### 4. Liability Assessment
- Could this feature create legal or regulatory exposure?
- Does AI output need human review before user-facing display?
- Are there accuracy guarantees implied that can't be met?

### 5. Premature Optimization
- Are we building infrastructure for 10K users with 10?
- Could a simpler solution (hardcoded, manual, partial) serve the same need?
- Is the abstraction level appropriate for the current scale?

## Output Format

For each finding:
- **Severity:** 🔴 Critical (blocks plan) / 🟡 Important (should address) / 🟢 Minor (nice to have)
- **Feature:** Which feature is affected
- **Finding:** What's wrong
- **Evidence:** Why you believe this (calculation, reference, precedent)
- **Recommendation:** Specific action to take

End with a **verdict:** one of:
- ✅ **Approved** — Plan is sound, proceed as designed
- ⚠️ **Approved with changes** — Address critical/important findings, then proceed
- ❌ **Rethink** — Fundamental issues require plan restructuring
```

---

## Handling Critique Results

### Adoption Framework

For each finding, classify:

| Classification | Criteria | Action |
|---|---|---|
| **Adopt** | Prevents a real failure (bug, math error, legal risk, data loss) | Update plan immediately |
| **Defer** | Valid concern but adds complexity without Day-1 benefit | Acknowledge in plan notes, add to Phase 2+ |
| **Reject** | Based on incorrect assumption or misunderstanding of context | Document rejection rationale |

### Documentation Template

```markdown
### Rubber-Duck Review Summary

**Verdict:** ⚠️ Approved with changes

| # | Severity | Finding | Decision | Rationale |
|---|----------|---------|----------|-----------|
| 1 | 🔴 Critical | Revenue math incorrect for Feature X | Adopted | Arithmetic disproven; feature deferred |
| 2 | 🟡 Important | No ops staff for Feature Y | Adopted | Moved to Phase 2 with staffing prerequisite |
| 3 | 🟢 Minor | Could simplify Feature Z architecture | Deferred | Simplification adds maintenance burden later |
```
