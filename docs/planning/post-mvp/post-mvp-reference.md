# Post-MVP Backlog Reference

> Cross-reference for `docs\planning\task-checklist.md`.
> Use this file to map each deferred checklist item to the detailed planning documents that define scope, sequencing, and acceptance criteria.

---

## Source set

| Document | Role |
|---|---|
| `v1.1-roadmap.md` | Primary task breakdown for tc-12 through tc-14 |
| `post-mvp-patterns-analysis.md` | Architecture rationale, priority, and pattern trade-offs for tc-12 through tc-18 |
| `post-mvp-implementation-guide.md` | Execution sequence, dependencies, and rollout guidance for tc-12 through tc-14 |
| `..\task-checklist.md` | Authoritative status tracker for whether work is still pending or complete |

---

## Ownership rule

1. Update `task-checklist.md` when status changes.
2. Update one or more `post-mvp\*.md` files when scope, sequencing, or acceptance criteria change.
3. When a post-MVP task becomes active implementation work, add or update the target architecture doc listed below.

---

## Task map

| Task | Release wave | Summary | Detailed sources | Target reference docs |
|---|---|---|---|---|
| `tc-12` | v1.1 | Event deduplication for Stripe retries | `v1.1-roadmap.md` Phase 1, `post-mvp-patterns-analysis.md` section 1, `post-mvp-implementation-guide.md` Phase 1 | `docs\architecture\patterns\event-deduplication.md` |
| `tc-13` | v1.1 | Event sourcing and transaction timeline | `v1.1-roadmap.md` Phase 2, `post-mvp-patterns-analysis.md` section 2, `post-mvp-implementation-guide.md` Phase 2 | `docs\architecture\patterns\event-sourcing.md` |
| `tc-14` | v1.1 | Outbox pattern and delivery guarantees | `v1.1-roadmap.md` Phase 3, `post-mvp-patterns-analysis.md` section 3, `post-mvp-implementation-guide.md` Phase 3 | `docs\architecture\patterns\outbox-pattern.md`, update `docs\architecture\event-bus\event-bus.md` |
| `tc-15` | v1.2 | Saga orchestration for long-running workflows | `post-mvp-patterns-analysis.md` section 4 | `docs\architecture\patterns\saga-pattern.md` |
| `tc-16` | v1.2 | Dead letter queue for poison/unprocessable events | `post-mvp-patterns-analysis.md` section 5 | `docs\architecture\patterns\dead-letter-queue.md` |
| `tc-17` | v1.3+ | Event enrichment for downstream consumers | `post-mvp-patterns-analysis.md` section 6 | `docs\architecture\patterns\event-enrichment.md` |
| `tc-18` | v1.3+ | Circuit breaker / provider resilience | `post-mvp-patterns-analysis.md` section 7 | `docs\architecture\patterns\circuit-breaker.md` |

---

## Checklist alignment

The checklist now groups Post-MVP work into three execution bands:

| Checklist track | Scope |
|---|---|
| **Track D** | v1.1 advanced webhook reliability (`tc-12` to `tc-14`) |
| **Track E** | v1.2 workflow recovery and ops tooling (`tc-15` to `tc-16`) |
| **Track F** | v1.3+ optimization and provider resilience (`tc-17` to `tc-18`) |

Items that remain in the "Additional deferred backlog" stay there until they get their own planning doc under `docs\planning\post-mvp\`.

---

## Update checklist

When any Post-MVP work moves forward, verify all of the following:

1. `task-checklist.md` status changed from `[ ]` to `[x]` for the completed sub-task.
2. The source planning doc still matches the implementation approach.
3. The target architecture or operations reference doc exists and is linked from the appropriate README.
4. Any cross-cutting docs affected by the change are updated in the same pass.

---

**Last updated:** 2026-04-29 21:06 EDT
