# 14 — Consultant Dashboard

> Self-service portal for consultants (payees) to track secured funds and earnings.

## Overview

The **Consultant Dashboard** is the primary interface for consultants who receive escrow-protected payments. It provides visibility into funds secured for their services, total earnings from released payments, and active client engagements.

## User Stories

1. **As a consultant**, I want to see how much money is currently held in escrow for my services so I can plan my cash flow.
2. **As a consultant**, I want to track my total earnings from released escrow payments.
3. **As a consultant**, I want to see all my active engagements (held + pending transactions) to manage my workload.
4. **As a consultant**, I want to click on a transaction to view details and request release when work is delivered.

## Page Layout

```
┌─────────────────────────────────────────────────────┐
│ Consultant Dashboard                                │
│ View your secured funds and active engagements      │
├──────────────┬──────────────┬───────────────────────┤
│ Funds        │ Total        │ Pending               │
│ Secured:     │ Earned:      │ Delivery:             │
│ $7,500.00    │ $15,200.00   │ 2 engagements         │
├──────────────┴──────────────┴───────────────────────┤
│ Active Engagements                                  │
│ ┌────┬──────────┬────────────────┬────────┬───────┐ │
│ │ ID │ Amount   │ Client         │ Status │  ▶    │ │
│ │ 42 │ $5,000   │ c@corp.com     │ Held   │ View  │ │
│ │ 50 │ $2,500   │ d@startup.io   │ Pending│ View  │ │
│ └────┴──────────┴────────────────┴────────┴───────┘ │
└─────────────────────────────────────────────────────┘
```

## Component Structure

```
Components/Pages/Dashboard/
├── ConsultantDashboard.razor      ← Markup (earnings cards + engagement table)
├── ConsultantDashboard.razor.cs   ← Logic (MediatR queries, auth check)
└── ConsultantDashboard.razor.css  ← Scoped styles
```

## Data Flow

```
ConsultantDashboard
    │ OnInitializedAsync
    ▼
IMediator.Send(ListTransactionsQuery { FilterByConsultant = currentUserEmail })
    │
    ▼ IReadOnlyList<EscrowTransactionDto>
    │
    ├── Summary: sum held (secured), sum released (earned), count pending
    └── Engagement table: active transactions with client info
```

## Authentication & Authorization

- **Route:** `/dashboard/consultant`
- **Auth:** `[Authorize]` — requires authenticated user
- **Data filtering:** Transactions filtered by `ConsultantEmail == currentUser.Email`

> ⚠️ **Same auth gap as Client Dashboard** — user login must be implemented first.

## Consultant-Specific Considerations

### Revenue Visibility
Consultants care about **cash flow** — the dashboard emphasizes:
- **Funds Secured:** Money held in escrow awaiting delivery (future cash)
- **Total Earned:** Released funds (realized revenue)
- **Pending Delivery:** Work that needs to be completed to trigger release

### Payout Gap
> ⚠️ **Current limitation:** Releasing funds updates the transaction status but does not actually transfer money to the consultant's bank account. **Stripe Connect** (destination charges or transfers) is required for real consultant payouts. This is a post-MVP enhancement.

## Localization

Resource files at `Resources/Components/Pages/Dashboard/`:
- `ConsultantDashboard.resx` (en-US)
- `ConsultantDashboard.es.resx` (es-MX)

## Future Enhancements

- [ ] Stripe Connect onboarding for real payouts
- [ ] Earnings chart (monthly/quarterly trends)
- [ ] Invoice generation from released transactions
- [ ] "Request Release" button that notifies the client
- [ ] Consultant profile / public portfolio page
