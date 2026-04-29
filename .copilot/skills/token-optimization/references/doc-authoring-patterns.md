# Doc Authoring Patterns — Token-Optimized

> Reference for: `.github/skills/workflow/token-optimization/SKILL.md`
> Load when: Writing feature docs, ADRs, planning docs, or architecture docs.

---

## Pattern 1 — Self-Contained Feature Doc

```markdown
# NN — Feature Name

> Status: **Planned** | **In Progress** | **Implemented**
> Last synced: YYYY-MM-DD
> Cross-references: [Related](../path.md) · [Another](../path.md)

## Summary

| Aspect | Detail |
|--------|--------|
| Purpose | One sentence |
| Layer | Application / Infrastructure / Presentation |
| Dependencies | What must exist first |
| Effort | S / M / L |
| Risk | Low / Medium / High |

## How It Works

[Step table — not prose]

| Step | Action | Component |
|------|--------|-----------|
| 1 | User does X | UI Component |
| 2 | Handler validates | MediatR Handler |
| 3 | Strategy executes | Strategy impl |
| 4 | State persisted | Repository |
| 5 | Event published | EventBus |

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Provider | Azure OpenAI | Cost, latency, compliance |
| Pattern | Strategy | OCP — swap providers without code changes |

## Files Involved

| Layer | File | Purpose |
|-------|------|---------|
| Application | `Features/X/Command.cs` | MediatR command |
| Infrastructure | `Services/X/Impl.cs` | Provider implementation |

## Security Considerations

[Bullet list — 3-5 items max]

## Testing Strategy

| Test Type | What | Tool |
|-----------|------|------|
| Unit | Handler logic | xUnit + Moq |
| Integration | API endpoint | WebApplicationFactory |
```

**Why this works:**
- Agent reads Summary table (10 tokens) to decide relevance
- Step table gives full flow without reading source code
- Key Decisions table prevents re-debating settled choices
- Files Involved table tells agent exactly where to look
- Total: ~1.5K tokens for a complete feature understanding

---

## Pattern 2 — Architecture Decision Record (ADR)

```markdown
# ADR-NNN — Decision Title

> Status: **Accepted** | **Proposed** | **Superseded by ADR-NNN**
> Date: YYYY-MM-DD
> Cross-references: [Context](../path.md)

## Context

[2-3 sentences. What problem prompted this decision?]

## Decision

[1-2 sentences. What did we decide?]

## Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Option A | Fast | Brittle | Doesn't scale |
| Option B | Flexible | Complex | Over-engineered for MVP |

## Consequences

### Positive
- [bullet]
- [bullet]

### Negative
- [bullet]

### Neutral
- [bullet]

## Implementation Notes

[Only if the decision has non-obvious implementation implications — 3-5 bullets max]
```

**Token savings vs prose ADR:** ~40% fewer tokens. Table-based alternatives eliminate verbose comparison paragraphs.

---

## Pattern 3 — Planning Doc (Status Tracking)

```markdown
# Phase Name — Status

> Last synced with codebase: YYYY-MM-DD

## Progress

| Task | Status | Blocker |
|------|--------|---------|
| Feature A | ✅ Done | — |
| Feature B | 🔄 In Progress | Waiting on X |
| Feature C | ⬜ Planned | Depends on B |

## What's Built

[Bullet list of completed items with file paths]

## What's Missing

[Bullet list of remaining items — actionable, not vague]

## Gate Criteria

| Criterion | Met? |
|-----------|------|
| Core flow works E2E | ✅ |
| Tests pass | ⬜ |
| Docs updated | ⬜ |
```

---

## Pattern 4 — Index / Catalog Doc

```markdown
# Category — Document Index

> Last updated: YYYY-MM-DD | Count: N items

| Name | Path | Purpose | Status |
|------|------|---------|--------|
| Feature A | `features/a/a.md` | Does X | Implemented |
| Feature B | `features/b/b.md` | Does Y | Planned |
```

**Key:** The index enables agents to find the right doc without listing directories or grepping. One table scan = O(1) token cost vs O(N) file reads.

---

## Cross-Reference Rules

### DO — Link to source of truth
```markdown
> Cross-references: [Payment Strategies](../architecture/payment-strategies/payment-strategies.md)
```

### DON'T — Copy content from another doc
```markdown
<!-- ❌ This paragraph is duplicated from payment-strategies.md -->
The Strategy Pattern uses ISP-compliant interfaces: IFundHoldable, IFundReleasable...
```

### When to summarize vs link
| Situation | Action |
|-----------|--------|
| Reader needs 1-line context | One-sentence summary + link |
| Reader needs full detail | Link only (no summary) |
| Content is shared by 3+ docs | Create a dedicated doc, link from all |
| Content changes frequently | Always link (avoid stale copies) |

---

## Size Guidelines

| Artifact | Target | Max | Over Max Action |
|----------|--------|-----|-----------------|
| Feature doc | 4–6 KB | 8 KB | Split into main + references/ |
| ADR | 3–4 KB | 6 KB | Trim alternatives table |
| Planning doc | 5–8 KB | 10 KB | Archive completed phases |
| Index/catalog | 2–4 KB | 6 KB | Split by category |
| Skill core | 6–8 KB | 10 KB | Move details to references/ |
| Skill reference | 3–4 KB | 5 KB | Split by subtopic |
| Agent instructions | 8–12 KB | 15 KB | Use `applyTo` scoped files |
