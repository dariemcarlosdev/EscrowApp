# 15 — Transaction Detail

> Detailed view of a single escrow transaction with action controls.

## Overview

The **Transaction Detail** page shows the complete state of an escrow transaction — participants, amount, status, payment provider reference, and available actions. It's the primary page for executing state transitions (release, dispute, cancel).

## User Stories

Stories for the Transaction Detail page — the primary screen where parties act on a transaction (release, dispute, cancel). Action buttons are state-dependent.

### Story 1 — Authorized party views their transaction

**As a** Client or Consultant, **I want** to open `/transaction/{id}` and see the full state of a transaction I am a party to, **so that** I can verify amounts, status, and counterparty details before acting.

**Acceptance Criteria:**

- [ ] GetTransactionQuery returns the EscrowTransactionDto for that ID
- [ ] I see ClientEmail, ConsultantEmail, Amount, Status, ServiceDescription, CreatedAt, and ExternalReference
- [ ] user-facing copy uses approved terminology (no "escrow")

```gherkin
Feature: Authenticated transaction view
  Scenario: Party views their own transaction
    Given I am authenticated as a participant in transaction 42
    When I navigate to /transaction/42
    Then GetTransactionQuery returns the EscrowTransactionDto for that ID
    And I see ClientEmail, ConsultantEmail, Amount, Status, ServiceDescription, CreatedAt, and ExternalReference
    And user-facing copy uses approved terminology (no "escrow")
```

### Story 2 — Action buttons reflect status

**As a** Client, **I want** to see only the actions that are valid for the current transaction status, **so that** I cannot attempt an illegal state transition.

**Acceptance Criteria:**

- [ ] "Release Funds", "Raise Dispute", and "Cancel Hold" are all visible
- [ ] "Release Funds" and "Cancel Hold" are hidden or disabled
- [ ] only resolution-related actions are available

```gherkin
Feature: State-dependent action visibility
  Scenario: Funded (Held) transaction
    Given I view a transaction in status "Funded (Held)"
    When the page renders
    Then "Release Funds", "Raise Dispute", and "Cancel Hold" are all visible

  Scenario: Disputed transaction
    Given I view a transaction in status "Disputed"
    When the page renders
    Then "Release Funds" and "Cancel Hold" are hidden or disabled
    And only resolution-related actions are available
```

### Story 3 — Unauthorized access is blocked

**As a** Platform Admin, **I want** users who are not parties to a transaction (and not admins) to be unable to view its detail page, **so that** transaction privacy is preserved.

**Acceptance Criteria:**

- [ ] the request is rejected (403 Forbidden or redirect to dashboard)
- [ ] no transaction details are rendered in the response

```gherkin
Feature: Authorization on transaction detail
  Scenario: Non-party user
    Given I am authenticated as a user who is neither client nor consultant on transaction 42
    When I navigate to /transaction/42
    Then the request is rejected (403 Forbidden or redirect to dashboard)
    And no transaction details are rendered in the response
```


## Page Layout

```
┌─────────────────────────────────────────────────────┐
│ ← Dashboard    Transaction #42                      │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Client:      client@example.com                    │
│  Consultant:  consultant@dev.io                     │
│  Amount:      $5,000.00                             │
│  Status:      ● Funded (Held)                       │
│  Service:     Backend API development               │
│  Created:     2025-01-15 14:30 UTC                  │
│  Provider:    Stripe (pi_3abc123...)                 │
│                                                     │
│  ┌─────────────┐ ┌───────────────┐ ┌──────────────┐│
│  │ Release Funds│ │ Raise Dispute │ │ Cancel Hold  ││
│  └─────────────┘ └───────────────┘ └──────────────┘│
│                                                     │
│  Activity Timeline                                  │
│  ─────────────────                                  │
│  (Coming in future update)                          │
│                                                     │
└─────────────────────────────────────────────────────┘
```

## Component Structure

```
Components/Pages/Dashboard/
├── TransactionDetail.razor        ← Markup (detail card + action buttons)
├── TransactionDetail.razor.cs     ← Logic (load by ID, handle actions)
└── TransactionDetail.razor.css    ← Scoped styles
```

## Data Flow

```
TransactionDetail (route: /dashboard/transaction/{Id:int})
    │ OnInitializedAsync
    ▼
IMediator.Send(GetTransactionQuery { TransactionId = Id })
    │
    ▼ EscrowTransactionDto
    │
    ├── Display: all transaction fields
    └── Actions: Release / Dispute / Cancel (conditional on status)
```

## Action Buttons — State-Dependent Visibility

| Status | Release | Dispute | Cancel |
|--------|---------|---------|--------|
| Pending | ❌ | ❌ | ❌ |
| Funded (Held) | ✅ | ✅ | ✅ |
| Released | ❌ | ❌ | ❌ |
| Disputed | ❌ | ❌ | ❌ |
| Cancelled | ❌ | ❌ | ❌ |

## Action Flows

### Release Funds
```
Button click → Confirmation dialog → IMediator.Send(ReleaseFundsCommand)
→ Success: redirect to dashboard with success toast
→ Failure: show error alert
```

### Raise Dispute
```
Button click → Dispute reason modal → IMediator.Send(DisputeFundsCommand)
→ Success: refresh page showing Disputed status
→ Failure: show error alert
```

### Cancel Hold
```
Button click → Cancellation reason modal → IMediator.Send(CancelFundsCommand)
→ Success: redirect to dashboard with cancellation confirmation
→ Failure: show error alert
```

## Authentication & Authorization

- **Route:** `/dashboard/transaction/{Id:int}`
- **Auth:** `[Authorize]` — requires authenticated user
- **Access control:** Only the client or consultant on the transaction may view it
- Must verify `ClientEmail == currentUser || ConsultantEmail == currentUser`

## Localization

Resource files at `Resources/Components/Pages/Dashboard/`:
- `TransactionDetail.resx` (en-US)
- `TransactionDetail.es.resx` (es-MX)

## Future Enhancements

- [ ] Activity timeline showing all state transitions with timestamps
- [ ] File attachments (contracts, deliverables)
- [ ] Chat/messaging between client and consultant
- [ ] Partial release (milestone-based payments)
- [ ] Transaction receipt / PDF export
