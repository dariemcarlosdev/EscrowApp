# 19 — Platform Fee

> ⚠️ **Compliance-sensitive — requires legal review before production deployment.**
> Revenue-critical feature. The word "escrow" must not appear in user-facing copy. Use "service fee" or "platform fee" in all UI strings.

## Overview

The **Platform Fee** is NexTruzt.io's primary Day-1 revenue mechanism. A configurable percentage (default **1.5%**) is calculated on every escrow amount at the time the funds are authorized (held), and included in the total charge to the client.

**Business rationale (from `docs/business/business-model/business-model.md`):**
- On a $5,000 project → NexTruzt.io earns **$75.00** at 1.5%
- Comparable to Escrow.com's 1–3.25% (but without the $25 minimum floor)
- **13× cheaper than Upwork** (1.5% vs 20%), which is the primary competitive pitch

```
Client holds $5,000 for consulting project
├── Stripe fee (2.9% + $0.30): $145.30   ← passed through to Stripe
├── NexTruzt.io platform fee (1.5%): $75.00   ← retained on platform account
├── Consultant receives: $4,779.70
└── NexTruzt.io net revenue: $75.00
```

## User Stories

Stories for the configurable platform fee — NexTruzt.io's Day-1 revenue mechanism. **User-facing copy must say *platform fee* or *service fee* — never *escrow fee*.**

### Story 1 — Transparent fee preview before authorization

**As a** Client, **I want** to see the platform fee broken out from the principal amount before I authorize a hold, **so that** I know exactly what NexTruzt.io retains and what the consultant will receive.

**Acceptance Criteria:**

- [ ] the UI shows "Platform fee: $75.00"
- [ ] the UI shows "Consultant receives: $4,925.00" (excluding Stripe pass-through fees)
- [ ] the wording uses "platform fee" or "service fee" — never "escrow fee"

```gherkin
Feature: Fee transparency on the create form
  Scenario: Quote calculation
    Given the platform fee rate is 1.5%
    When I enter a project amount of $5,000.00
    Then the UI shows "Platform fee: $75.00"
    And the UI shows "Consultant receives: $4,925.00" (excluding Stripe pass-through fees)
    And the wording uses "platform fee" or "service fee" — never "escrow fee"
```

### Story 2 — Fee captured only on release

**As a** Platform Admin, **I want** the platform fee to be captured to the platform Stripe account only when funds are released, **so that** cancelled or disputed transactions correctly produce zero revenue.

**Acceptance Criteria:**

- [ ] the platform Stripe account retains $75.00 as application_fee_amount
- [ ] the consultant's connected account receives the principal minus the fee
- [ ] the Stripe hold is fully voided
- [ ] no application fee is captured

```gherkin
Feature: Fee captured on release
  Scenario: Release captures principal and fee
    Given a transaction in "Funded (Held)" with PlatformFee=$75.00
    When ReleaseFundsCommand succeeds
    Then the platform Stripe account retains $75.00 as application_fee_amount
    And the consultant's connected account receives the principal minus the fee

  Scenario: Cancellation produces zero fee revenue
    Given a transaction in "Funded (Held)" with PlatformFee=$75.00
    When CancelFundsCommand succeeds
    Then the Stripe hold is fully voided
    And no application fee is captured
```

### Story 3 — Audit-stable percentage on each transaction

**As a** Compliance Officer, **I want** the `PlatformFeePercentage` used for each transaction to be persisted at creation time and never overwritten, **so that** future rate changes do not retroactively alter historical revenue records.

**Acceptance Criteria:**

- [ ] transaction 42 still reports PlatformFeePercentage=0.015
- [ ] historical revenue reports are unchanged

```gherkin
Feature: Immutable fee percentage per transaction
  Scenario: Rate change does not affect history
    Given transaction 42 was created with PlatformFeePercentage=0.015
    When the platform rate is later updated to 0.02 in configuration
    Then transaction 42 still reports PlatformFeePercentage=0.015
    And historical revenue reports are unchanged
```

### Story 4 — Fee rate is configuration, not code

**As a** Developer, **I want** the platform fee rate to be configurable via the Options pattern (`appsettings.json` / environment variables), **so that** product can tune the rate per environment without a code change or redeploy of business logic.

**Acceptance Criteria:**

- [ ] the resolved fee for a $1,000 transaction is $20.00
- [ ] no recompilation is required

```gherkin
Feature: Configurable fee rate
  Scenario: Rate changes via configuration
    Given PlatformFeeOptions:Rate is set to 0.02 in appsettings.{Environment}.json
    When the application starts and the next CreateAndHoldFundsCommand is processed
    Then the resolved fee for a $1,000 transaction is $20.00
    And no recompilation is required
```


---

## Revenue Gate Status

> This is **Revenue Blocker #1** in the MVP task checklist. No revenue is generated until this feature is live.

- **Break-even:** ~14 transactions/month at $2,500 avg (platform fee only)
- **Month 1-3 target:** $600/month revenue at 20 transactions
- **Configuration:** Per-environment via Options pattern — no code change required to adjust the rate

---

## Domain Model Changes

### New fields on `EscrowTransaction`

| Field | Type | Purpose |
|---|---|---|
| `PlatformFee` | `decimal` | Calculated fee amount in dollars (e.g., `75.00`) |
| `PlatformFeePercentage` | `decimal` | Rate applied at calculation time (e.g., `0.015`) — immutable after creation for audit trail |

> **Audit rule:** `PlatformFeePercentage` is stored at creation time so that future rate changes do not retroactively alter historical records. This satisfies the regulatory traceability requirement.

### EF Core Migration

A new migration must be created after adding the fields:

```bash
dotnet ef migrations add AddPlatformFeeToEscrowTransaction
dotnet ef database update
```

---

## Configuration

### `appsettings.json`

```jsonc
{
  "Platform": {
    "FeePercentage": 0.015,   // 1.5% default
    "MinimumFee": 0.50,       // $0.50 minimum (prevents edge cases on micro-transactions)
    "Currency": "USD"
  }
}
```

### Options class (to be created)

```csharp
// Infrastructure/Options/PlatformOptions.cs
public sealed record PlatformOptions
{
    public const string SectionName = "Platform";

    public decimal FeePercentage { get; init; } = 0.015m;
    public decimal MinimumFee { get; init; } = 0.50m;
    public string Currency { get; init; } = "USD";
}
```

### DI Registration (`Program.cs`)

```csharp
services.Configure<PlatformOptions>(
    builder.Configuration.GetSection(PlatformOptions.SectionName));
```

---

## Fee Calculation Logic

Fee calculation lives exclusively in `CreateAndHoldFundsHandler`. No other handler calculates fees — this enforces SRP.

```
platformFee = max(escrowAmount × feePercentage, minimumFee)
totalCharge  = escrowAmount + platformFee
```

**Fintech guardrail:** Fee amounts are **never modified directly by consumers** — they flow from the domain model to Stripe via the handler. Raw arithmetic on payment values outside the handler is prohibited per `AGENTS.md`.

### Handler implementation sketch

```csharp
// Features/Escrow/CreateAndHoldFunds/CreateAndHoldFundsHandler.cs
var options = _platformOptions.Value;
var platformFee = Math.Max(
    command.EscrowAmount * options.FeePercentage,
    options.MinimumFee);

var transaction = new EscrowTransaction
{
    Amount              = command.EscrowAmount,
    PlatformFee         = platformFee,
    PlatformFeePercentage = options.FeePercentage,
    // ...
};

// Stripe is charged the full amount (escrow + fee)
await _strategyFactory
    .GetStrategy<IFundHoldable>()
    .HoldFundsAsync(command.EscrowAmount + platformFee, command.IdempotencyKey, cancellationToken);
```

---

## MediatR Flow (updated)

```
CreateAndHoldFundsCommand
├── EscrowAmount (decimal, required — escrow portion only)
├── ClientEmail (string, required)
├── ConsultantEmail (string, required)
├── Description (string, required)
└── IdempotencyKey (string, required)

    ↓ CreateAndHoldFundsHandler

1. Validate command (FluentValidation — see input-validation feature doc)
2. Load PlatformOptions from IOptions<PlatformOptions>
3. Calculate platformFee = max(amount × rate, minFee)
4. Create EscrowTransaction entity (amount, fee, fee %, status = Pending)
5. Persist via IEscrowTransactionRepository (get database ID)
6. Call IFundHoldable.HoldFundsAsync(amount + platformFee, idempotencyKey)
7. Update transaction status → "Funded (Held)"
8. Persist updated status
9. Publish PaymentReceivedEvent (include platformFee in event payload)
10. Return CreateAndHoldFundsResult(transactionId, escrowAmount, platformFee)
```

---

## Domain Event

`PaymentReceivedEvent` must be extended (or a new event created) to carry fee data for the audit trail:

```csharp
// Events/PaymentReceivedEvent.cs
public sealed record PaymentReceivedEvent(
    int TransactionId,
    decimal EscrowAmount,
    decimal PlatformFee,          // ← new field
    decimal PlatformFeePercentage, // ← new field for immutable audit
    string ClientEmail,
    string ConsultantEmail,
    DateTimeOffset OccurredAt) : DomainEvent;
```

> **Regulatory note:** The audit trail must capture the fee and rate at the moment of creation. This supports dispute resolution and potential regulatory reporting requirements.

---

## API Response

The `CreateAndHoldFundsResult` DTO should surface fee details to the caller:

```jsonc
// POST /api/escrow/create-and-hold — 201 Created
{
  "transactionId": 42,
  "escrowAmount": 5000.00,
  "platformFee": 75.00,
  "platformFeePercentage": 0.015,
  "totalCharged": 5075.00,
  "status": "Funded (Held)"
}
```

---

## Files

| File | Action | Purpose |
|---|---|---|
| `Models/EscrowTransaction.cs` | **Modify** | Add `PlatformFee`, `PlatformFeePercentage` fields |
| `Migrations/` | **Create** | `AddPlatformFeeToEscrowTransaction` migration |
| `Infrastructure/Options/PlatformOptions.cs` | **Create** | Typed options record |
| `Features/Escrow/CreateAndHoldFunds/CreateAndHoldFundsHandler.cs` | **Modify** | Inject `IOptions<PlatformOptions>`, calculate fee, charge total |
| `Features/Escrow/CreateAndHoldFunds/CreateAndHoldFundsResult.cs` | **Modify** | Add `PlatformFee`, `PlatformFeePercentage`, `TotalCharged` |
| `Events/PaymentReceivedEvent.cs` | **Modify** | Include fee fields in event payload |
| `Program.cs` | **Modify** | Register `PlatformOptions` via `services.Configure<>` |
| `appsettings.json` | **Modify** | Add `Platform` section |
| `appsettings.Production.json` | **Modify** | Add `Platform` section placeholder |

---

## Business Rules

1. Platform fee is **always** calculated — there is no zero-fee path in the MVP
2. `MinimumFee` ($0.50) prevents edge cases on transactions smaller than $33 (where 1.5% < $0.50)
3. `PlatformFeePercentage` is **immutable** after creation — stored for audit, not recalculated on release
4. Fee is included in the Stripe authorization total — client is charged `escrowAmount + platformFee`
5. On release, consultant receives `escrowAmount - stripeFee` — the platform fee remains in NexTruzt.io's Stripe balance
6. On cancellation, Stripe voids the entire authorization (including fee) — NexTruzt.io collects nothing on cancelled transactions

---

## Future Enhancements (Post-MVP backlog)

| Enhancement | Trigger |
|---|---|
| Express Payout fee (0.5%, min $1) | 50+ transactions/month |
| Instant Payout fee (1.5%, min $2) | 50+ transactions/month |
| Per-tier fee rates (Starter 2.5%, Professional 1.5%, Business 1.0%) | v1.1 pricing tiers |
| Fee revenue dashboard | Admin ops > 10/week |
| Monthly revenue reports + accounting export | Post-MVP |
