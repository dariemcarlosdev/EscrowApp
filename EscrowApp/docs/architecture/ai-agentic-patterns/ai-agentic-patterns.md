# AI Agentic Design Patterns — NexTruzt.io Integration Plan

> Status: **Planned** (pending — blocked by core MVP completion)
> Last synced with codebase: 2026-04-10
> Cross-references: [AI Strategy ADR](../ai-strategy/ai-strategy.md) · [AI Features Roadmap](../../features/ai-features/ai-features-roadmap.md) · [Event Bus](../event-bus/event-bus.md) · [Payment Strategies](../payment-strategies/payment-strategies.md)

---

## Overview

This document maps the **5 canonical AI Agentic Design Patterns** to the NexTruzt.io EscrowApp architecture. Each pattern is evaluated against the existing Clean Architecture + CQRS/MediatR stack with concrete integration points, implementation scope, and dependency requirements.

**Source:** Data Science Dojo — "Top AI Agentic Design Patterns" (2026)

### Prerequisite Gate

> 🔴 **No agentic AI work may begin until the 8 core MVP tasks are complete.** Real money must flow end-to-end before adding AI agent capabilities. See [Implementation Plan](../../planning/implementation-plan.md#-mvp-release--ship-to-charge).

---

## Pattern 1: Reflection Pattern

> Output is fed back into the model so it can critique and refine its own response.

### Flow

```
User Query → LLM Generate → Initial Response → LLM Reflect → Reflected Output
                                                    ↑                │
                                                    └── Iterate N ───┘
                                              → Final Response
```

### EscrowApp Integration: Domain Invariant Validation

**Use case:** Self-validating payment decisions — before committing a state transition, an AI agent reviews its own decision against domain rules and refines.

**Integration points:**

| Component | Role | Existing File |
|-----------|------|---------------|
| `MediatR Pipeline Behavior` | Intercept handler responses for reflection | `Infrastructure/Middleware/LoggingBehavior.cs` (pattern reference) |
| `EscrowTransaction` state machine | Domain rules to reflect against | `Models/EscrowTransaction.cs` |
| `FluentValidation` | First-pass validation (pre-handler) | `Features/Escrow/*/Validator.cs` (planned) |

**Proposed implementation:**

```
ReflectionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
├── Executes handler → gets initial response
├── Passes response to reflection prompt (domain rules + response)
├── If reflection finds issues → logs warning, optionally blocks
└── Returns validated response
```

**Key scenarios:**
- Validate AI-generated service descriptions against regulatory terminology rules (no "escrow" in user-facing copy)
- Review dispute resolution suggestions against the state machine (disputed tx cannot be released)
- Check AI-generated transaction summaries for PII leakage before serving to UI

**Effort:** S (Small) — New `IPipelineBehavior`, ~100 LOC
**Depends on:** AI Strategy ADR layer placement, `IAiTextGenerationService` interface

---

## Pattern 2: Tool Use Pattern

> The model decides which tools to call, retrieves results, and builds its final answer.

### Flow

```
User Query → LLM → Access Tools [Web Search, Vector DB, APIs] → LLM Generate → Response
```

### EscrowApp Integration: MediatR Handlers as AI Tools

**Use case:** An external AI agent (or MCP client) calls EscrowApp's MediatR handlers as tools to orchestrate payment flows programmatically.

**Integration points:**

| MediatR Handler | AI Tool Name | Operation |
|----------------|-------------|-----------|
| `CreateAndHoldFundsCommand` | `hold_funds` | Authorize + hold payment |
| `ReleaseFundsCommand` | `release_funds` | Capture held funds |
| `DisputeFundsCommand` | `dispute_funds` | Flag + cancel hold |
| `CancelFundsCommand` | `cancel_funds` | Void hold (TODO — not yet implemented) |
| `GetTransactionQuery` | `get_transaction` | Read transaction state |
| `ListTransactionsQuery` | `list_transactions` | Query transaction portfolio |

**Proposed implementation:**

```
Option A: MCP Server (Model Context Protocol)
├── Register each MediatR handler as an MCP tool
├── AI agents discover tools via MCP protocol
├── Authentication via existing ApiKeyAuthenticationHandler
└── Idempotency keys passed through tool parameters

Option B: OpenAI Function Calling Schema
├── Generate JSON Schema from MediatR command records
├── Expose via /api/ai/tools endpoint
├── AI agents call existing REST API (/api/escrow/*)
└── API key auth already in place
```

**Security requirements:**
- All tool invocations must pass through `[Authorize(Policy = "ApiAccess")]`
- Idempotency keys MUST be required on all payment mutation tools
- Tool responses must never include PII (emails, payment method IDs)
- Rate limiting on AI tool endpoints (separate from human API limits)

**Effort:** M (Medium) — MCP server or function-calling schema, ~300 LOC
**Depends on:** `CancelFundsHandler` completion, API auth hardening

---

## Pattern 3: Reason and Act (ReAct) Pattern

> The model alternates between reasoning and taking actions until the task is complete.

### Flow

```
User Query → LLM → Reason ↔ Act (Tools + API) → LLM Generate → Response
                      ↑          │
                      └── Loop ──┘
```

### EscrowApp Integration: Payment Lifecycle Automation

**Use case:** An AI agent monitors the escrow lifecycle and autonomously manages state transitions based on external signals (delivery confirmation, time elapsed, dispute evidence).

**Integration points:**

| Observation Source | Reasoning Rule | Action |
|-------------------|---------------|--------|
| `GetTransactionQuery` | Status is `Held` + delivery confirmed | `ReleaseFundsCommand` |
| `DisputeRaisedEvent` | Dispute has evidence, under threshold | Suggest auto-resolution |
| `ListTransactionsQuery` | Transaction held > 30 days | Notify parties, suggest release or cancel |
| Stripe webhook | `payment_intent.payment_failed` | Retry or escalate |

**Proposed implementation:**

```
PaymentLifecycleAgent
├── Observe: Subscribe to IEventBus (PaymentReceivedEvent, DisputeRaisedEvent)
├── Reason: Evaluate state against business rules
│   ├── "Transaction held for 30+ days → notify parties"
│   ├── "Dispute under $100 → auto-resolve via platform policy"
│   └── "Delivery confirmed by both parties → release"
├── Act: Dispatch MediatR commands
│   ├── ReleaseFundsCommand (with idempotency key)
│   ├── DisputeFundsCommand (with reason)
│   └── Notification commands (future)
└── Loop: Continue observing until transaction reaches terminal state
```

**Critical safeguards:**
- Human-in-the-loop approval gate for any release > configurable threshold
- All automated actions logged with full audit trail via domain events
- Circuit breaker: max N automated actions per transaction before escalating to human
- Dispute auto-resolution limited to pre-approved policy rules only

**Effort:** L (Large) — Event subscription, reasoning engine, action dispatch, safeguards
**Depends on:** `InMemoryEventBus` upgrade to MassTransit, Stripe webhook handler (#7)

---

## Pattern 4: Planning Pattern

> A planning module breaks the goal into subtasks, executes them, and adjusts the plan based on results.

### Flow

```
User Goal → Planner → Subtask 1, 2, 3 → Executor → Replan if needed → Final Response
```

### EscrowApp Integration: Multi-Step Escrow Orchestration

**Use case:** Complex escrow workflows that require multiple steps with dynamic replanning — e.g., milestone-based payments, multi-party escrows, conditional releases.

**Integration points:**

| Component | Planner Role | Existing Pattern |
|-----------|-------------|-----------------|
| `IPaymentStrategyFactory` | Dynamic strategy resolution for replanning | `Services/Strategies/PaymentStrategyFactory.cs` |
| `CreateAndHoldFundsHandler` | Atomic multi-step execution (create + hold) | `Features/Escrow/CreateAndHoldFunds/` |
| `EscrowTransaction` state machine | Defines valid subtask sequences | `Models/EscrowTransaction.cs` |
| Domain events | Trigger replanning on state changes | `Events/PaymentReceivedEvent.cs`, `Events/DisputeRaisedEvent.cs` |

**Proposed implementation:**

```
EscrowWorkflowPlanner
├── Goal: "Complete $5,000 consulting escrow"
├── Plan:
│   ├── Subtask 1: Verify client identity (KYC — future)
│   ├── Subtask 2: Hold funds (CreateAndHoldFundsCommand)
│   ├── Subtask 3: Monitor delivery milestones
│   └── Subtask 4: Release or dispute based on outcome
├── Execute: Dispatch subtasks sequentially via IMediator
├── Replan:
│   ├── If hold fails (Stripe error) → retry with Polly, or switch provider via factory
│   ├── If disputed → switch to dispute resolution workflow
│   └── If partial delivery → split release (future milestone feature)
└── Complete: All subtasks resolved, terminal state reached
```

**Replanning triggers:**
- Stripe API failure → Polly retry → fallback to alternative provider (OCP via `IPaymentStrategyFactory`)
- Dispute raised mid-workflow → pause release subtask, inject mediation subtask
- Timeout exceeded → notify parties, extend deadline or auto-cancel

**Effort:** XL (Extra Large) — Workflow engine, state persistence, replanning logic
**Depends on:** Milestone-based escrow domain model (Post-PMF Phase 3)

---

## Pattern 5: Multi-Agent (Supervisor) Pattern

> A supervisor agent routes tasks to specialized worker agents and compiles the final response.

### Flow

```
User Prompt → Supervisor → Worker 1 (Task 2a) → Result
                         → Worker 2 (Task 2b) → Result  → Supervisor → Response
                         → Worker 3 (Task 2c) → Result
```

### EscrowApp Integration: Specialized Agent Fleet

**Use case:** A supervisor agent coordinates specialized workers for complex operations spanning compliance, fraud detection, settlement, and audit — each worker is an expert in its domain.

**Integration points:**

| Worker Agent | Responsibility | Event Trigger | Tools Used |
|-------------|---------------|---------------|------------|
| Compliance Worker | Scan for "escrow" in user-facing copy, verify regulatory rules | On deploy / PR | `grep` on `.resx`, `.razor` files |
| Fraud Detection Worker | Analyze transaction patterns, flag anomalies | `PaymentReceivedEvent` | `ListTransactionsQuery`, heuristic rules |
| Settlement Worker | Execute fund releases after approval | `PaymentReceivedEvent` + human approval | `ReleaseFundsCommand` |
| Audit Worker | Subscribe to ALL domain events, build audit trail | All `DomainEvent` subclasses | `IEventBus` consumer |
| Dispute Triage Worker | Classify disputes, suggest resolution path | `DisputeRaisedEvent` | `GetTransactionQuery`, resolution rules |

**Proposed implementation:**

```
EscrowSupervisorAgent (Infrastructure/Agents/)
├── Receives: User request or domain event
├── Routes to specialized workers:
│   ├── ComplianceWorker → validates regulatory constraints
│   ├── FraudWorker → analyzes transaction patterns
│   ├── SettlementWorker → executes payment operations
│   ├── AuditWorker → records everything for compliance
│   └── DisputeTriageWorker → classifies and routes disputes
├── Aggregates: Merges worker results, resolves conflicts
└── Returns: Unified response with all worker findings
```

**Architecture alignment:**
- Workers implement domain strategy interfaces (ISP-compliant)
- Communication via upgraded `IEventBus` (MassTransit queues per worker)
- Each worker is independently deployable and testable
- Supervisor uses `IPaymentStrategyFactory` pattern for worker resolution

**Effort:** XL (Extra Large) — Multi-service architecture, queue infrastructure, worker implementations
**Depends on:** `InMemoryEventBus` → MassTransit upgrade, all 5 workers designed and tested

---

## Implementation Priority & Dependencies

### Dependency Graph

```
                    ┌─────────────────────────────┐
                    │  Core MVP (8 tasks)          │
                    │  MUST complete first         │
                    └──────────┬──────────────────┘
                               │
              ┌────────────────┼────────────────┐
              ▼                ▼                ▼
    ┌─────────────┐  ┌──────────────┐  ┌──────────────┐
    │ CancelFunds │  │ EventBus     │  │ Stripe       │
    │ Handler     │  │ Upgrade      │  │ Webhook      │
    │ (MVP #2)    │  │ (InMemory →  │  │ (MVP #7)     │
    │             │  │  MassTransit)│  │              │
    └──────┬──────┘  └──────┬──────┘  └──────┬───────┘
           │                │                │
           ▼                ▼                ▼
    ┌─────────────┐  ┌──────────────┐  ┌──────────────┐
    │ Tool Use    │  │ ReAct        │  │ Reflection   │
    │ Pattern     │  │ Pattern      │  │ Pattern      │
    │ (MCP/Func)  │  │ (Lifecycle)  │  │ (Pipeline)   │
    └──────┬──────┘  └──────┬──────┘  └──────────────┘
           │                │
           ▼                ▼
    ┌──────────────────────────────┐
    │ Multi-Agent (Supervisor)     │
    │ Pattern                      │
    └──────────────┬───────────────┘
                   │
                   ▼
    ┌──────────────────────────────┐
    │ Planning Pattern             │
    │ (Workflow Orchestration)      │
    └──────────────────────────────┘
```

### Priority Matrix

| # | Pattern | Effort | Value | Priority | Gate |
|---|---------|--------|-------|----------|------|
| 1 | **Reflection** (Pipeline Behavior) | S | High — catches regulatory/PII violations before they ship | P1 — First AI agent pattern | AI Strategy ADR + `IAiTextGenerationService` |
| 2 | **Tool Use** (MCP Server) | M | High — enables any LLM to operate the escrow system | P1 — Parallel with #1 | `CancelFundsHandler` complete |
| 3 | **ReAct** (Lifecycle Automation) | L | Medium — automates routine state transitions | P2 — After #1 and #2 | EventBus upgrade + Stripe webhook |
| 4 | **Multi-Agent** (Supervisor Fleet) | XL | Medium — coordination layer for specialized workers | P3 — After #3 | MassTransit + worker implementations |
| 5 | **Planning** (Workflow Orchestration) | XL | Low (MVP) — milestone escrows are Post-PMF | P4 — Post-PMF only | Milestone domain model designed |

---

## Infrastructure Prerequisites

| Prerequisite | Current State | Target State | Blocks |
|-------------|--------------|-------------|--------|
| `InMemoryEventBus` | Logs only, no subscribers | MassTransit with consumer queues | ReAct, Multi-Agent |
| `CancelFundsHandler` | `NotImplementedException` | Full cancel + refund flow | Tool Use |
| Stripe webhook | Not implemented | `payment_intent.succeeded` handler | ReAct |
| `IAiTextGenerationService` | Interface defined (AI Strategy ADR) | Azure OpenAI implementation | Reflection |
| Rate limiting | Not implemented | Per-endpoint rate limits | Tool Use (AI agent throttling) |
| MCP Server framework | Not present | `EscrowMcpServer` project | Tool Use (Option A) |

---

## Security Considerations

All AI agentic patterns must comply with the existing OWASP security posture:

| Concern | Requirement | OWASP Category |
|---------|-------------|----------------|
| Agent authentication | AI agents authenticate via API key or managed identity | A07 — Auth Failures |
| Tool authorization | Each tool invocation checked against `[Authorize(Policy)]` | A01 — Broken Access Control |
| PII in agent context | Never pass emails, payment IDs, or secrets to LLM context | A02 — Cryptographic Failures |
| Idempotency | All payment tool calls require idempotency keys | Fintech guardrail |
| Audit trail | Every AI-initiated action emits a domain event | A09 — Logging Failures |
| Human-in-the-loop | Payment releases above threshold require human approval | A04 — Insecure Design |
| Prompt injection | Validate all AI-generated text before persisting or rendering | A03 — Injection |

---

## Regulatory Compliance

> ⚠️ AI agents must comply with all regulatory rules from [AGENTS.md](../../../../AGENTS.md#regulatory-compliance--critical).

- AI-generated user-facing text must be scanned for the word "escrow" before rendering
- AI agents must not claim NexTruzt.io is a licensed escrow agent or money transmitter
- All AI-initiated payment operations must be traceable via domain events for regulatory audit
- AI dispute resolution is advisory only — final decisions require human approval until legal framework is established

---

## Related Documentation

| Doc | Relationship |
|-----|-------------|
| [AI Strategy ADR](../ai-strategy/ai-strategy.md) | Layer placement and interface contracts for AI services |
| [AI Features Roadmap](../../features/ai-features/ai-features-roadmap.md) | What AI features to build (this doc covers **how** — agentic patterns) |
| [Event Bus](../event-bus/event-bus.md) | Current `InMemoryEventBus` — prerequisite for ReAct and Multi-Agent |
| [Payment Strategies](../payment-strategies/payment-strategies.md) | ISP interfaces used as AI tool contracts |
| [API Integration](../api-integration/api-integration.md) | REST API endpoints exposed as AI tools |
| [Implementation Plan](../../planning/implementation-plan.md) | MVP tasks that must complete before AI agentic work |
| [Task Checklist](../../planning/task-checklist.md) | Granular task tracking for AI agentic implementation |
