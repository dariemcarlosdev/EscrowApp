# Consensus Analysis Patterns — Multi-Agent Planner

> Reference for Step 3 of the Core Workflow. Load only when analyzing cross-agent results.

---

## Building the Consensus Matrix

### Step 1: Normalize Rankings

Agents return different formats. Normalize to a 3-level scale:

| Symbol | Meaning |
|--------|---------|
| ✅ | Agent recommends building (now or soon) |
| ⚠️ | Agent has conditions or reservations |
| ❌ | Agent recommends deferring or avoiding |

### Step 2: Map Agreement Levels

| Pattern | Agreement Level | Action |
|---------|----------------|--------|
| ✅✅✅ | **Strong consensus — Build** | Plan with high confidence |
| ✅✅⚠️ | **Conditional consensus** | Address the condition, then build |
| ✅⚠️⚠️ | **Weak consensus** | Investigate conditions before committing |
| ✅✅❌ | **Split — Dissent matters** | The ❌ agent may see a risk others miss; investigate |
| ✅⚠️❌ | **No consensus** | Needs human decision or deeper analysis |
| ❌❌❌ | **Strong consensus — Defer** | Defer with high confidence |

### Step 3: Resolve Disagreements

For each split or no-consensus feature:

1. **Identify the dissenting rationale** — Why does agent X disagree?
2. **Check for factual errors** — Is the dissent based on incorrect assumptions?
3. **Check for perspective blindness** — Is one lens seeing something the others can't?
4. **Determine if the dissent is addressable** — Can a constraint be relaxed or a risk mitigated?
5. **Document the resolution** — State which agent was right and why

---

## Common Disagreement Patterns

### Pattern: Business says "Critical" but UX says "Avoid"

**Root cause:** Feature generates revenue but creates friction or damages trust.
**Resolution:** Check if the feature can be internal-only (preserves revenue signal without user-facing friction).
**Example:** Risk scoring — business wants fraud prevention, UX sees opaque scores as trust-damaging. Solution: internal-only scoring dashboard.

### Pattern: Technical says "Ship" but Business says "Defer"

**Root cause:** Feature is easy to build but doesn't affect revenue.
**Resolution:** Apply MVP gatekeeper — can we launch without it? If yes, defer regardless of effort.
**Example:** AI-generated tooltips — trivial to implement but no revenue impact.

### Pattern: UX says "Must-have" but Technical says "High effort"

**Root cause:** Great user experience requires significant engineering investment.
**Resolution:** Check for an MVP version that delivers 80% of the UX value at 20% of the effort.
**Example:** Full RAG chatbot (high effort) vs. curated FAQ widget (low effort, 80% of value).

### Pattern: All agents agree but rubber-duck disagrees

**Root cause:** Rubber-duck applies operational/practical constraints that idealized analyses miss.
**Resolution:** Take the rubber-duck seriously — it's checking math, operations, and prerequisites.
**Example:** "Milestone escrow = 5× revenue" — all agents agreed until rubber-duck checked the arithmetic.

---

## Output Template

```markdown
## Cross-Agent Consensus Matrix

| Feature | Business | Technical | UX | Rubber-Duck | Agreement | Final Phase |
|---------|----------|-----------|-----|-------------|-----------|-------------|
| Feature A | ✅ #1 | ✅ Ship (3d) | ✅ Must-have | ✅ Approved | Strong | Phase 1 |
| Feature B | ✅ #2 | ⚠️ Medium | ❌ Low delight | ⚠️ Conditional | Split | Phase 2 |
| Feature C | ❌ Defer | ✅ Ship (1d) | ✅ Quick win | ❌ No prerequisite | Split | Phase 2 |

### Key Disagreements Resolved

| Disagreement | Resolution | Rationale |
|---|---|---|
| Business says B is #2 but UX says avoid | Deferred to Phase 2 | UX friction outweighs revenue signal at launch |
| Technical says C is easy but rubber-duck blocks | Phase 2 gate | Prerequisite (webhook sync) not yet complete |
```
