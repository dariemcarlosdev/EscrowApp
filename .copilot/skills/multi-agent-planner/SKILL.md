---
name: multi-agent-planner
description: "Plan features by launching parallel sub-agents with different LLMs, comparing their analyses, and synthesizing the best plan via adversarial critique"
license: MIT
allowed-tools: Read, Grep, Glob, Bash, task, read_agent, write_agent
metadata:
  version: "1.0.0"
  domain: project-management
  triggers: plan features, multi-agent, compare models, AI planning, feature roadmap, multi-LLM, cross-model analysis
  role: planner
  scope: orchestration
  platforms: copilot-cli, claude, gemini
  output-format: plan
  related-skills: mvp-gatekeeper, feature-forge, spec-writer, agent-orchestrator
---

# Multi-Agent Planner

You are a **Planning Orchestrator** that produces high-quality feature roadmaps by leveraging **diverse AI perspectives**. Instead of relying on a single model's reasoning, you launch parallel sub-agents with different LLMs, compare their analyses, and synthesize the best plan — then stress-test it with adversarial critique.

## When to Use This Skill

- Planning a feature set for a new product or MVP phase
- Prioritizing a backlog of 5+ candidate features
- Making strategic decisions that affect product direction
- When a single perspective is insufficient (complex tradeoffs, unknown territory)
- When you need to justify prioritization decisions with evidence from multiple angles

---

## Core Workflow

### Step 1 — Establish Context (5 min)

Before launching agents, gather:

1. **Product state** — What exists today? What's the MVP status?
2. **Revenue model** — How does the product make money?
3. **User personas** — Who are the primary users?
4. **Candidate features** — What features are being considered?
5. **Constraints** — Budget, timeline, team size, regulatory

✅ **Checkpoint:** You can describe the product, its users, its revenue model, and 5+ candidate features in one paragraph each.

### Step 2 — Launch Parallel Sub-Agents (3 agents, different models)

Launch **exactly 3** sub-agents in parallel, each with a different model and analytical lens:

| Agent | Recommended Model | Lens | Prompt Focus |
|-------|-------------------|------|--------------|
| **Business Strategist** | Claude Sonnet 4.5 | Revenue & differentiation | "Which features generate revenue, reduce churn, or create competitive moats?" |
| **Technical Architect** | GPT-5.x | Architecture & effort | "For each feature: effort estimate, architecture impact, risk level, dependencies" |
| **UX Designer** | GPT-5.4 / Gemini | User experience & adoption | "Map features to user personas. Prioritize by delight-to-effort ratio." |

**Rules for sub-agent prompts:**
- Provide identical feature lists to all 3 agents
- Include full product context (from Step 1) in each prompt
- Ask each agent to produce a **ranked list** with explicit reasoning
- Ask each agent to identify **one thing the other lenses might miss**
- Set a clear output format: table + 1-paragraph rationale per feature

✅ **Checkpoint:** 3 agents launched in parallel with different models and distinct analytical lenses.

### Step 3 — Collect and Compare (cross-agent synthesis)

When all agents complete, build a **consensus matrix**:

```
| Feature | Business | Technical | UX | Agreement |
|---------|----------|-----------|-----|-----------|
| Feature A | ✅ #1 | ✅ Ship (3d) | ✅ Must-have | Strong |
| Feature B | ✅ #2 | ⚠️ High effort | ❌ Low delight | Split |
| Feature C | ❌ Defer | ✅ Ship (1d) | ✅ Quick win | Split |
```

**Identify:**
- **Strong consensus** (3/3 agree) — high confidence, plan as recommended
- **Split consensus** (2/3 agree) — needs deeper analysis; document the dissent
- **Disagreements** (all different) — flag for human decision or rubber-duck review

✅ **Checkpoint:** Consensus matrix built. Agreements and disagreements documented with rationale.

### Step 4 — Adversarial Critique (rubber-duck)

Send the synthesized plan to a **rubber-duck agent** with this prompt structure:

```
Here is our plan from 3 independent analyses:
[consensus matrix + phase breakdown]

You are a constructively adversarial reviewer. Find:
1. Math errors — do revenue claims hold up under arithmetic?
2. Operational blind spots — do we have the staff/infra to operate this?
3. Dependency gaps — are prerequisites actually met?
4. Liability risks — could this feature create legal/regulatory exposure?
5. Premature optimization — are we building for 10K users with 10?
```

**Handle critique:**
- **Adopt** findings that prevent real failures (bugs, math errors, legal risk)
- **Acknowledge but defer** findings that add complexity without clear Day-1 benefit
- **Reject with justification** findings based on false assumptions

✅ **Checkpoint:** Rubber-duck review complete. Each finding addressed with adopt/defer/reject decision.

### Step 5 — Produce Final Plan

Synthesize into a phased plan with:

1. **Phase 0** — Prerequisites that must be complete before any planned features
2. **Phase 1** — Highest-consensus, lowest-risk feature(s)
3. **Phase 2** — Post-launch features requiring real user data
4. **Phase 3** — Strategic features requiring product-market fit

For each feature, document:
- **Cross-agent vote** (who agreed, who dissented, why)
- **Gate criteria** (what must be true before implementation begins)
- **Architecture preview** (which layers are affected, new interfaces needed)
- **Effort estimate** (range from technical agent, validated by critique)
- **Risk assessment** (from all perspectives combined)

✅ **Checkpoint:** Plan has clear phases, gate criteria, and every feature decision is traceable to cross-agent evidence.

### Step 6 — Document

Create two outputs:

1. **Feature roadmap document** — standalone, cross-referenced to architecture and planning docs
2. **Planning doc updates** — add entries to the project's task checklist and implementation plan

**Documentation rules:**
- Each doc is self-contained (loadable independently by any AI agent)
- Use cross-references (links), not duplicated content
- Tables over prose for scannable decision records
- Include the consensus matrix so future sessions understand the "why"

✅ **Checkpoint:** Documentation created. Cross-references verified. No orphan docs.

---

## Reference Guide

| Topic | File | Load When |
|-------|------|-----------|
| Agent prompt templates | `references/agent-prompts.md` | Writing sub-agent prompts for Step 2 |
| Consensus analysis patterns | `references/consensus-patterns.md` | Analyzing disagreements in Step 3 |
| Critique prompt templates | `references/critique-prompts.md` | Sending plan to rubber-duck in Step 4 |

---

## Anti-Patterns

| Anti-Pattern | Why It Fails |
|---|---|
| Launching agents without shared context | Agents optimize for different goals; results are incomparable |
| Using the same model for all agents | Eliminates model diversity — the whole point is different reasoning styles |
| Skipping rubber-duck critique | Plans look great until someone checks the math |
| Implementing without Phase 0 gate | Building AI before the core product works = engineering vanity |
| Consensus = average | Don't average rankings. Identify **why** agents disagree and resolve the conflict |
| Ignoring dissent | The dissenting agent often catches the blind spot everyone else missed |

---

## Token Optimization

This skill is designed for context-window efficiency:

- Sub-agents run in **separate context windows** — they don't consume your main context
- The consensus matrix is a **compact summary** — load it instead of re-reading agent outputs
- Reference files are **lazy-loaded** — only read when you're at that workflow step
- Final plan document is **self-contained** — future sessions load one file, not the full analysis

---

## Portability

This skill is **domain-agnostic**. It works for:
- Feature planning for any product (SaaS, mobile, fintech, e-commerce)
- Technology selection (comparing frameworks, services, architectures)
- Strategic planning (market entry, pricing, partnership decisions)
- Any decision where multiple expert perspectives reduce bias

Replace the agent lenses (Business/Technical/UX) with whatever perspectives matter for your domain:
- **Security Analyst** for compliance-heavy products
- **Data Engineer** for data-intensive products
- **Growth Marketer** for consumer apps
- **Domain Expert** for specialized industries
