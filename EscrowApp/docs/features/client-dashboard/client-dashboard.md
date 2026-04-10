# 13 — Client Dashboard

> Self-service portal for clients (payers) to manage their escrow payments.

## Overview

The **Client Dashboard** is the primary interface for clients who deposit funds into escrow. It provides visibility into active holds, released payments, and disputed transactions — enabling clients to manage their financial commitments without contacting support.

## User Stories

1. **As a client**, I want to see a summary of my escrow activity (active holds, released, disputed, total) so I can track my financial exposure.
2. **As a client**, I want to view all my transactions in a table so I can monitor each engagement's status.
3. **As a client**, I want to click on a transaction to see its full details and take actions (release, dispute, cancel).
4. **As a client**, I want to create a new escrow payment directly from the dashboard.

## Page Layout

```
┌─────────────────────────────────────────────────────┐
│ My Escrow Payments                                  │
│ Track your escrow transactions and manage payments  │
├──────────┬──────────┬──────────┬────────────────────┤
│ Active   │ Released │ Disputed │ Total Escrowed     │
│ Holds: 3 │ 5        │ 1        │ $12,500.00         │
├──────────┴──────────┴──────────┴────────────────────┤
│ Your Transactions                    [+ New Escrow] │
│ ┌────┬──────────┬────────────────┬────────┬───────┐ │
│ │ ID │ Amount   │ Consultant     │ Status │  ▶    │ │
│ │ 42 │ $5,000   │ j@consult.com  │ Held   │ View  │ │
│ │ 38 │ $2,500   │ a@dev.io       │ Released│ View │ │
│ └────┴──────────┴────────────────┴────────┴───────┘ │
└─────────────────────────────────────────────────────┘
```

## Component Structure

```
Components/Pages/Dashboard/
├── ClientDashboard.razor          ← Markup (summary cards + transaction table)
├── ClientDashboard.razor.cs       ← Logic (MediatR queries, auth check)
└── ClientDashboard.razor.css      ← Scoped styles (Bootstrap 5 cards)
```

## Data Flow

```
ClientDashboard
    │ OnInitializedAsync
    ▼
IMediator.Send(ListTransactionsQuery { FilterByClient = currentUserEmail })
    │
    ▼ IReadOnlyList<EscrowTransactionDto>
    │
    ├── Summary cards: count by status, sum amounts
    └── Transaction table: sortable, clickable rows → /dashboard/transaction/{id}
```

## Authentication & Authorization

- **Route:** `/dashboard/client`
- **Auth:** `[Authorize]` — requires authenticated user
- **Data filtering:** Transactions filtered by `ClientEmail == currentUser.Email`

> ⚠️ **Current gap:** Only API key auth exists in Program.cs. User login (Entra ID or ASP.NET Identity) must be implemented before this dashboard is functional. See auth skeleton pages in `Components/Pages/Auth/`.

## Localization

Resource files at `Resources/Components/Pages/Dashboard/`:
- `ClientDashboard.resx` (en-US) — default English strings
- `ClientDashboard.es.resx` (es-MX) — Spanish translations

Key strings: `PageTitle`, `PageSubtitle`, `ActiveHolds`, `Released`, `Disputed`, `TotalEscrowed`, `TransactionsTitle`, `NewEscrow`, `NoTransactionsYet`

## Future Enhancements

- [ ] Real-time updates via SignalR when transaction status changes
- [ ] Pagination / infinite scroll for large transaction lists
- [ ] Export transactions to CSV/PDF
- [ ] "Create Escrow" modal directly from dashboard
- [ ] Transaction search and filtering (by status, date range, consultant)
