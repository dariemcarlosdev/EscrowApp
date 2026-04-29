# Revenue Gates — Deep Dive

> Load this reference when evaluating whether a feature or task generates revenue.

## Gate 1 — Transaction Revenue Path

Every feature MUST map to one of these revenue events:

| Revenue Event | Trigger | Amount |
|---|---|---|
| **Platform Fee** | Client creates escrow hold | 1.5% of transaction amount |
| **Express Payout** (post-MVP) | Consultant requests instant payout | Flat $5.00 or 1% |
| **Dispute Resolution** (post-MVP) | Arbitration service invoked | Flat $25.00 |

### How to Map a Feature to Revenue

Ask: "If this feature didn't exist, would a client still be able to escrow $5,000 and a consultant receive $4,779.70?"

- **YES** → The feature is not revenue-critical. DEFER.
- **NO** → The feature is revenue-critical. BUILD.

### Revenue-Critical Feature Inventory (MVP)

These are the ONLY features that must exist for Day-1 revenue:

| # | Feature | Revenue Dependency |
|---|---|---|
| 1 | User registration / login | Users must exist to transact |
| 2 | Create escrow transaction | Entry point for money flow |
| 3 | Hold funds (Stripe manual capture) | Authorization = money committed |
| 4 | Release funds (capture PaymentIntent) | Capture = revenue collected |
| 5 | Platform fee calculation | 1.5% fee = NexTruzt.io revenue |
| 6 | Transaction dashboard (client) | Client must see transaction status |
| 7 | Transaction dashboard (consultant) | Consultant must see payment status |
| 8 | Basic dispute flow | Trust mechanism = user retention |
| 9 | Email notification (hold/release) | Users must know money moved |
| 10 | Cancel escrow (void hold) | Unhappy path = prevent chargebacks |

**Everything else is v1.1+.**

## Gate 2 — Security as Revenue Protection

Security features protect revenue by preventing:

| Threat | Revenue Impact | Required Security |
|---|---|---|
| Unauthorized access | Fraudulent transactions | `[Authorize]` + policy-based auth |
| Payment tampering | Double charges, lost funds | Idempotency keys + amount validation |
| Data breach | Regulatory fines, trust loss | HTTPS, HSTS, no PII in logs |
| Injection attacks | Database compromise | Parameterized queries only |
| CSRF | Unauthorized state changes | Antiforgery tokens |

**Rule:** Security features are ALWAYS revenue-critical because a single breach kills the business.

## Gate 3 — Revenue Validation Checklist

Before any sprint or work session, score the backlog:

```
For each item in the backlog:
  1. Revenue impact?     [direct / indirect / none]
  2. Users affected?     [all / some / hypothetical]
  3. Time to build?      [<2h / 2-8h / >8h]
  4. Dependencies?       [none / 1-2 / complex chain]

Priority = (revenue_impact × users_affected) / (time × dependencies)

Score:
  direct=3, indirect=1, none=0
  all=3, some=2, hypothetical=0
  <2h=1, 2-8h=2, >8h=3
  none=1, 1-2=2, complex=3
```

**Anything scoring 0 in revenue_impact is automatically DEFERRED.**

## Gate 4 — Post-MVP Revenue Expansion (DO NOT BUILD YET)

These are documented for strategic awareness ONLY. Building any of these before MVP ships is a VIOLATION:

| Feature | Revenue Model | Build When |
|---|---|---|
| Express Payout | $5 or 1% per instant payout | After 50+ transactions/month |
| Dispute Arbitration | $25 per arbitration case | After 10+ disputes/month |
| Premium Dashboard | $29/month subscription | After 100+ active users |
| API Access | Usage-based pricing | After 3+ integration requests |
| Multi-currency | Higher fees on FX transactions | After international user demand |
| Escrow Templates | Freemium with premium templates | After user feedback on templates |
| Web3 Bridge | Gas fees + platform premium | After crypto market validation |

**The trigger column is the permission slip.** Until the trigger condition is met, the feature stays in the backlog.
