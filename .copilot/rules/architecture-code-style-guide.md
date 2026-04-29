---
trigger: always_on
---

# Architecture & Coding Guidelines (Master Rules)

This document serves as the "Source of Truth" for our engineering standards as we develop the Escrow Prototype. As an AI Assistant pairing with a Senior Engineer, I will strictly adhere to these rules moving forward.

## 0. Project Vision & Web3 Migration Strategy
**Primary Goal:** Build with v3 Migration in mind. 
While the current MVP focuses on the Web2 path (Stripe, Plaid), we must enforce strict architectural decoupling (SOLID and Design Patterns) from day one to allow for a seamless V3 migration, which will involve Blockchain and Smart Contracts.
**Core Rule:** Ensure all domain logic is entirely provider-agnostic. The core Escrow Engine must never care whether the transaction is executing in USD via Stripe or ETH via a Smart Contract.

### 0.1 Hybrid Architecture Pillars (MVP v3-Ready)
To avoid massive future refactors, we implement these three pillars under a **"Lean Abstraction"** strategy:

1.  **Hybrid Identity 🆔**:
    - **Rule**: Users are identified by an abstract `Actor` profile. 
    - **Implementation**: The database must support an `IdentityMapping` table. While we only use Email/OAuth today, the schema must be ready to link a `WalletAddress` without changing the `User` entity.
2.  **Normalized Events ⚡**:
    - **Rule**: Domain logic must react to *Internal Events*, not provider-specific callbacks.
    - **Implementation**: Create a `UnifiedEventBus`. Stripe Webhooks must be translated into a `PaymentReceivedEvent` before hitting the Business Layer. This allows us to plug in a Blockchain Indexer later with zero changes to the Core.
3.  **Agnostic Persistence 💾**:
    - **Rule**: All transaction references must be stored in generic fields.
    - **Implementation**: Use `string ExternalReference` instead of `string StripeSessionId`. The DB must be ready to store 64-character transaction hashes (Web3) or standard API IDs (Web2).

## 1. State of the Current Web2 Route

* **Status:** *Partially Decoupled (Needs Refactor for Production).*
* **Analysis:** We successfully took the first step of abstraction by creating the `IEscrowPaymentService` interface (complying with the Dependency Inversion Principle). **However**, the current implementation `StripeEscrowService` violates the Single Responsibility Principle (SRP). It is currently injecting `EscrowDbContext` directly and modifying local EF Core records while simultaneously orchestrating the remote Stripe API calls.
* **Goal:** A payment service should know nothing about the internal relational database. The `StripeEscrowService` must delegate all DB tracking to the Repository.

## 2. SOLID Principles Compliance

1. **Single Responsibility (SRP):** 
   - HTTP clients / SDK orchestrators handle *only* external integration.
   - Database tracking is moved to robust mechanisms.
2. **Open/Closed (OCP):** 
   - We must design the Escrow engine so that if we introduce a PayPal or Ethereum Smart Contract route tomorrow, we add new code, but do not alter the existing `StripeEscrowService`.
3. **Always Update Documentation:**
   - **MANDATORY RULE:** Any architectural or feature code change must be instantly accompanied by an update to the corresponding markdown files in the `docs/` directory to prevent structural rot.
4. **Interface Segregation (ISP):** 
   - Keep interfaces lean. Do not force an interface to implement `ReleaseFundsAsync` if a specific provider doesn't support manual capture.
5. **Dependency Inversion (DIP):** 
   - Always map abstractions to concrete implementations in `Program.cs`.

## 3. Approved OOP Design Patterns

Below are the mandatory patterns we will employ to decouple the architecture:

### A. The Repository Pattern
To eliminate direct DbContext access from our business layers, we will implement an `IEscrowTransactionRepository` for fetching and saving models.

### B. The Strategy Pattern (Payment Strategies)
To support the "Hybrid" approach (Web2 Stripe vs Web3 Crypto), we will implement a Strategy Pattern where the core Engine evaluates the transaction type and picks the right `IEscrowPaymentStrategy`.

### C. Application Service / Facade Pattern
We must build a pure business-logic layer (e.g., `EscrowManagerService`) that coordinates the Repository and the Payment Service. The Controller/UI calls this Manager, ensuring Controllers stay "thin".

## 4. System Design Best Practices
- **Idempotency:** All payment requests (Holds/Releases) must pass an Idempotency Key (supported heavily by Stripe) to prevent double-charging on network retries.
- **Pure Entities:** Models like `EscrowTransaction` remain oblivious to DTOs or API Request objects.
- **Lean Implementation Rule: Abstract today, implement tomorrow:** We will not program Wallet connections or Blockchain Indexers now, but the code must accept objects (Events/DTOs) that support them.


## 5. Blazor Component Architecture (UI Layer)

To ruthlessly enforce the **Single Responsibility Principle (SRP)** at the Frontend, our Blazor components must strictly adhere to the "Code-Behind" and CSS Isolation pattern rather than mixing concerns:

1. **`.razor` files (The View):**
   - Strictly reserved for HTML markup, Razor syntax directives, and Data Binding.
   - **Rule:** NO complex logic or data retrieval inside inline `@code { }` blocks. 
2. **`.razor.cs` files (Code-Behind / The ViewModel):**
   - Contains all presentation logic, state management, event handlers (`OnClick`, etc.), and Service Injection (`[Inject]`).
   - The class must be defined as a `partial class` matching the Razor view name.
3. **`.razor.css` files (Scoped Styling):**
   - Use Blazor CSS Isolation for component-specific styles to prevent global stylesheet bloat and styling collisions.
   - **Rule:** Avoid inline `style="..."` attributes entirely.