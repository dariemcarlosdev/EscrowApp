# 15 — Transaction Detail

> Detailed view of a single escrow transaction with action controls.

## Overview

The **Transaction Detail** page shows the complete state of an escrow transaction — participants, amount, status, payment provider reference, and available actions. It's the primary page for executing state transitions (release, dispute, cancel).

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
