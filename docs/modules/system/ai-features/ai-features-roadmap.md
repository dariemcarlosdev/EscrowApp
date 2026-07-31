# AI Features Roadmap — NexTruzt.io

> Phased AI feature plan validated by multi-model analysis and critique.
> Status: **Planned** (not implemented)
> Last synced with codebase: 2026-04-10
> Cross-references: [AI Architecture Strategy](../../../architecture/ai-strategy/ai-strategy.md) · [Implementation Plan](../../../planning/implementation-plan.md) · [Business Model](../../../business/business-model/business-model.md)

## Overview

This document is the phased product plan for AI-assisted features on NexTruzt.io. It enumerates which AI features are eligible for each phase, the prerequisite gate that must be cleared before any AI feature ships (the core money pipe must work end-to-end with real Stripe test cards), and the cross-agent rationale for prioritization. AI features are layered behind the same Strategy pattern as payment providers and **must never make autonomous decisions on regulated payment flows** — they assist users, never gatekeep money movement.

## User Stories

Stories framing the AI roadmap. AI features are *assistive*: they explain, suggest, and reduce friction — they never authorize, hold, release, or dispute funds. Compliance-sensitive: any AI-generated string surfaced to users must respect the *no escrow in user-facing copy* rule.

### Story 1 — Smart description assist (Phase 1)

**As a** Client, **I want** AI suggestions for the service description when I create a transaction, **so that** I can produce a clear scope without staring at a blank field.

**Acceptance Criteria:**

- [ ] the IAiTextGenerationService returns a candidate description
- [ ] the candidate is editable by me before submission
- [ ] the form still validates ServiceDescription length and required content

```gherkin
Feature: Description generation assist
  Scenario: User accepts a generated description
    Given I am on the create-transaction form
    And the AI assist is enabled
    When I provide a short brief and click "Suggest description"
    Then the IAiTextGenerationService returns a candidate description
    And the candidate is editable by me before submission
    And the form still validates ServiceDescription length and required content
```

### Story 2 — AI never authorizes payments

**As a** Compliance Officer, **I want** AI features to be strictly assistive and never call payment strategies, **so that** AI cannot move money or change a transaction's state autonomously.

**Acceptance Criteria:**

- [ ] no AI service references IFundHoldable, IFundReleasable, or IFundCancellable
- [ ] no AI handler publishes PaymentReceivedEvent or DisputeRaisedEvent

```gherkin
Feature: AI is assistive only
  Scenario: AI service has no payment dependencies
    Given the AI feature module
    When code analysis inspects its dependencies
    Then no AI service references IFundHoldable, IFundReleasable, or IFundCancellable
    And no AI handler publishes PaymentReceivedEvent or DisputeRaisedEvent
```

### Story 3 — AI is gated behind the core money pipe

**As a** Platform Admin, **I want** no AI feature to be released until the 8 core MVP tasks are complete and real money can flow end-to-end with a Stripe test card, **so that** AI never becomes a distraction from revenue-blocking work.

**Acceptance Criteria:**

- [ ] it is marked "blocked by MVP gate" in review
- [ ] no production deploy is approved

```gherkin
Feature: AI prerequisite gate
  Scenario: AI rollout blocked until core MVP is green
    Given any of the 8 core MVP tasks is not "done" in the implementation plan
    When an AI feature PR is opened
    Then it is marked "blocked by MVP gate" in review
    And no production deploy is approved
```

### Story 4 — AI feature is opt-in and easy to disable

**As a** Developer, **I want** every AI feature behind a feature flag in configuration, **so that** I can disable it instantly if it produces unsafe output or exceeds the cost budget.

**Acceptance Criteria:**

- [ ] the "Suggest description" affordance is not rendered
- [ ] no calls to IAiTextGenerationService are made

```gherkin
Feature: AI feature flag kill-switch
  Scenario: Flag disabled in configuration
    Given AiFeatures:DescriptionAssist:Enabled is set to false
    When a user opens the create-transaction form
    Then the "Suggest description" affordance is not rendered
    And no calls to IAiTextGenerationService are made
```


---

## Research Methodology

This roadmap was produced by **3 independent AI analyses** (different LLMs) + **1 adversarial critique**:

| Agent | Model | Lens | Key Insight |
|-------|-------|------|-------------|
| Business Strategist | Claude Sonnet 4.5 | Revenue impact | Milestone splitting is NOT a 5× revenue multiplier (math disproven) |
| Technical Architect | GPT-5.2 | Architecture fit | AI services must follow Strategy pattern; keep domain AI-free |
| UX Designer | GPT-5.4 | User delight | "AI should explain and assist — not decide or gatekeep" |
| Rubber-Duck Critic | Claude Sonnet 4.6 | Blind spots | Core money pipe must work before any AI ships |

---

## Prerequisite Gate

> 🔴 **No AI feature may be implemented until the 8 core MVP tasks are complete.**
> See [Implementation Plan — MVP Task Queue](../../../planning/implementation-plan.md).

**Gate criteria:** Real money flows end-to-end (create → hold → release with Stripe test card + platform fee collected).

---

## Phase 1 — Post-Core MVP (First AI Feature)

### Smart Service Description Generator

| Attribute | Value |
|-----------|-------|
| **What** | AI-assisted project description writing during transaction creation |
| **Why** | Removes blank-page friction (UX: "must-have"), low blast radius, no domain changes |
| **Who** | Both personas — Claire (clearer scope) and Marcus (professional proposals) |
| **Where** | Transaction creation flow — inline assist under description field |
| **Effort** | 2–4 days |
| **Cost** | ~$0.50/month at 10 users |
| **Risk** | Low — pre-transaction, purely assistive, easy to disable |

**Cross-agent consensus:** 3 of 4 agents agreed this is the safest first AI feature. Rubber-duck approved as "only acceptable AI experiment."

**Implementation:** See [AI Architecture Strategy](../../../architecture/ai-strategy/ai-strategy.md) for layer placement, interface design, and security constraints.

**New files when implemented:**

```
Features/Ai/GenerateDescription/
├── GenerateDescriptionCommand.cs
├── GenerateDescriptionHandler.cs
├── GenerateDescriptionResult.cs
└── GenerateDescriptionValidator.cs

Services/Ai/
├── IAiTextGenerationService.cs          (Application interface)
├── AzureOpenAiTextGenerationService.cs  (Infrastructure implementation)
└── AiServiceSettings.cs                 (Options pattern config)

Components/Shared/
├── ServiceDescriptionAssistant.razor
├── ServiceDescriptionAssistant.razor.cs
└── ServiceDescriptionAssistant.razor.css
```

---

## Phase 2 — Post-Launch (With Real User Data)

> **Trigger:** Platform is live, processing real transactions, webhook sync confirmed working.

### 2A. AI Transaction Summarizer

| Attribute | Value |
|-----------|-------|
| **What** | Plain-language "What's happening + what's next" card on transaction detail page |
| **Why** | UX rated "highest delight-to-effort"; zero additional friction |
| **Prerequisite** | Stripe webhook sync must be working correctly (wrong state = user liability) |
| **Effort** | 3–5 days |
| **Risk** | Medium — summarizing incorrect state causes trust damage |

**Deferred reason:** Rubber-duck critique — "If it summarizes payment state incorrectly before webhook sync works, users act on false information."

### 2B. Simple AI FAQ Widget

| Attribute | Value |
|-----------|-------|
| **What** | FAQ-based support assistant (curated answers, not full RAG) |
| **Why** | Defers support staff hiring; $30K–90K/year cost avoidance |
| **Prerequisite** | Measure actual support ticket volume first (may not be needed) |
| **Effort** | 2–3 days (FAQ-based); 8–15 days (full RAG — defer) |
| **Risk** | Low if FAQ-only; high if hallucinating payment advice |

### 2C. Heuristic Risk Scoring (Internal Only)

| Attribute | Value |
|-----------|-------|
| **What** | Rule-based transaction risk flags for internal admin review |
| **Why** | Stripe Radar covers fraud detection at Day-1; custom scoring needs baseline data |
| **Prerequisite** | 100+ transactions for baseline; operational staff to review flagged items |
| **Effort** | 3–5 days (heuristic); 8–12 days (with LLM classification) |
| **Risk** | High if user-facing (opaque scores damage trust); low if internal-only |

**Deferred reason:** UX agent placed in "Avoid" quadrant (low user delight, high effort). Business agent said "critical" but rubber-duck countered: "Stripe Radar already does this; who reviews scores with no ops staff?"

---

## Phase 3 — Post-PMF (6+ Months, Strategic Features)

> **Trigger:** Product-market fit confirmed, 500+ active users, dedicated ops team.

### 3A. Milestone-Based Escrow

| Attribute | Value |
|-----------|-------|
| **What** | Split one transaction into milestone-based partial payments |
| **Why** | Product redesign opportunity — reduces dispute risk via incremental delivery |
| **Critical finding** | Business agent claimed "5× fee multiplier" — **disproven by rubber-duck**: 1.5% of $5K = $75 regardless of split count. Multiple Stripe charges actually hurt margin ($0.30 fixed fee × N milestones). |
| **Risk** | Very high — new domain model (Milestone entity), state machine changes, partial releases, per-milestone disputes |

**This is a product redesign, not an AI feature.** If built, AI assists with milestone suggestion; the domain model change is the real work.

### 3B. AI Dispute Mediation

| Attribute | Value |
|-----------|-------|
| **What** | AI analyzes dispute evidence and suggests fair resolution options |
| **Why** | Resolves 30–40% of disputes without human intervention |
| **Prerequisite** | 100+ historical disputes for training data; legal review of AI recommendations |
| **Risk** | High — legal liability if AI suggests wrong resolution; "robot judge" perception |

### 3C. Predictive Cash Flow Dashboard

| Attribute | Value |
|-----------|-------|
| **What** | Forecast consultant earnings based on transaction pipeline |
| **Why** | Addresses Marcus's #1 pain point — cash flow uncertainty |
| **Prerequisite** | Sufficient transaction history per consultant for meaningful predictions |
| **Risk** | Medium — confidence labeling critical; users may over-rely on estimates |

### 3D. SOW/Invoice Generator

| Attribute | Value |
|-----------|-------|
| **What** | Generate professional Statements of Work and invoices from transaction data |
| **Why** | Value-add for consultants; potential premium feature |
| **Effort** | 10–18 days (document lifecycle + PDF rendering + permissions) |
| **Risk** | Medium — legal terms in generated documents need review |

---

## Cross-Agent Consensus Matrix

| Feature | Business | Technical | UX | Rubber-Duck | Final Phase |
|---------|----------|-----------|-----|-------------|-------------|
| Description Generator | Borderline MVP | ✅ Ship | ✅ Must-have | ✅ Only safe pick | **1** |
| Transaction Summarizer | — | ✅ Ship | ✅ Must-have | ⚠️ After webhooks | **2** |
| FAQ Widget | ✅ MVP | ❌ Defer | Nice-to-have | ❌ Premature | **2** |
| Risk Scoring | ✅ Critical | ✅ Heuristic | ❌ Avoid | ❌ Stripe Radar | **2** |
| Milestone Escrow | ✅ Revenue | — | — | ❌ Math wrong | **3** |
| Dispute Mediation | Post-MVP | ❌ Defer | Post-MVP | ❌ Defer | **3** |
| Cash Flow Dashboard | — | ❌ Defer | Post-MVP | ❌ Defer | **3** |
| SOW Generator | — | ❌ Defer | Nice-to-have | ❌ Defer | **3** |

---

## Key Lessons from Multi-Model Analysis

1. **Revenue claims need math verification.** The "5× fee multiplier" from milestones sounded compelling but was arithmetically false.
2. **UX and Business perspectives conflict on risk scoring.** Business sees risk reduction; UX sees friction and opacity. Resolution: internal-only, deferred.
3. **AI should assist, not decide.** All agents converged on this principle for fintech — AI suggestions with human final authority.
4. **Operational readiness matters.** Building a scoring engine without review staff creates dead data. Build features when the org can operate them.
5. **Webhook reliability is a prerequisite.** Any AI feature that summarizes or acts on payment state requires correct state first.
