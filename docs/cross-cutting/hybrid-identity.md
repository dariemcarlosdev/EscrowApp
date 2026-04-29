# Hybrid Identity Architecture — Actor ↔ ApplicationUser Bridge

**Last Updated:** 2026-04-16  
**Status:** ✅ Production-Ready (Track B Complete)

---

## Overview

NexTruzt.io implements a **hybrid Web2/Web3 identity architecture** using an `Actor ↔ ApplicationUser` bridge pattern. This design enables seamless integration of traditional ASP.NET Core Identity (Web2) with future blockchain wallet authentication (Web3).

**Core Principle:**  
`Actor` is the domain entity representing a user in the business model. `ApplicationUser` is the ASP.NET Core Identity entity managing authentication credentials. The bridge ensures both systems stay synchronized while maintaining clean separation of concerns.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                       Presentation Layer                     │
│                     (Blazor Components)                      │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│         RegisterCommandHandler (MediatR)                     │
│    ┌───────────────────────────────────────────┐            │
│    │ 1. Create Actor (domain entity)           │            │
│    │ 2. Create ApplicationUser (auth entity)   │            │
│    │ 3. Link via ActorId FK                    │            │
│    │ 4. Commit transaction atomically          │            │
│    └───────────────────────────────────────────┘            │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                       Domain Layer                           │
│                                                              │
│   ┌──────────────┐              ┌──────────────────────┐   │
│   │    Actor     │              │  ApplicationUser     │   │
│   │ (Business)   │◄─────────────│  (Authentication)    │   │
│   │              │   ActorId FK │                      │   │
│   ├──────────────┤              ├──────────────────────┤   │
│   │ Id (PK)      │              │ Id (PK)              │   │
│   │ DisplayName  │              │ UserName             │   │
│   │ WalletAddress│              │ Email                │   │
│   │ CreatedAt    │              │ PasswordHash         │   │
│   └──────────────┘              │ ActorId (FK)         │   │
│                                 │ SecurityStamp        │   │
│                                 └──────────────────────┘   │
│                                                              │
└──────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                       │
│               (EscrowDbContext + Identity)                   │
│                                                              │
│   ├── Actors Table (domain)                                 │
│   ├── AspNetUsers Table (Identity)                          │
│   │     └── ActorId FK → Actors.Id                          │
│   └── AspNetRoles, AspNetUserRoles, etc.                    │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## The Actor Model (Domain Entity)

**Location:** `EscrowApp/Models/Actor.cs`

```csharp
namespace EscrowApp.Models;

/// <summary>
/// Actor represents a user in the domain model — consultant, client, or admin.
/// Bridges to ApplicationUser via ActorId FK for hybrid Web2/Web3 identity.
/// </summary>
public sealed class Actor
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Web3 identity: Ethereum wallet address (0x...).
    /// Null for Web2-only users; populated when user links wallet.
    /// </summary>
    public string? WalletAddress { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation: One Actor can have one ApplicationUser
    public ApplicationUser? ApplicationUser { get; set; }
}
```

**Key Properties:**
- `Id` — Primary key; referenced by `ApplicationUser.ActorId` as the bridge FK
- `DisplayName` — User's public display name (consultants, clients)
- `WalletAddress` — Future Web3 integration; nullable until user links Ethereum wallet
- `CreatedAt` — Audit timestamp

---

## The ApplicationUser Model (Identity Entity)

**Location:** `EscrowApp/Models/ApplicationUser.cs`

```csharp
using Microsoft.AspNetCore.Identity;

namespace EscrowApp.Models;

/// <summary>
/// ApplicationUser extends IdentityUser with custom properties.
/// Bridges to Actor domain entity via ActorId FK (hybrid identity pattern).
/// </summary>
public sealed class ApplicationUser : IdentityUser<int>
{
    /// <summary>
    /// Foreign key to Actor (domain entity).
    /// Enables hybrid Web2/Web3 identity: ApplicationUser handles auth,
    /// Actor handles business logic.
    /// </summary>
    public int ActorId { get; set; }
    
    // Navigation property
    public Actor Actor { get; set; } = default!;
}
```

**Key Properties (inherited from `IdentityUser<int>`):**
- `Id` — Primary key (int, matches Actor.Id for consistency)
- `UserName` — Login identifier (set to email)
- `Email` — User's email address
- `PasswordHash` — Hashed password (ASP.NET Core Identity manages this)
- `SecurityStamp` — Invalidation token for password changes/logout
- `ActorId` — **BRIDGE**: Foreign key to `Actor.Id`

---

## The Bridge Pattern — Why It Matters

### Problem
- **Web2 authentication** (email/password, OAuth) requires ASP.NET Core Identity tables (`AspNetUsers`, `AspNetRoles`, etc.)
- **Web3 authentication** (wallet signatures, Ethereum accounts) requires blockchain addresses and signature validation
- **Domain logic** (secure payment holding, transactions, disputes) should be decoupled from authentication mechanism

### Solution
Use `ApplicationUser` as the authentication boundary and `Actor` as the business domain boundary. The `ActorId` FK links them.

**Benefits:**
1. **Separation of Concerns** — Domain logic never touches `IdentityUser`, `PasswordHash`, or authentication details
2. **Future Web3 Integration** — Adding wallet auth doesn't touch existing `ApplicationUser` infrastructure
3. **Clean Architecture Compliance** — Domain layer depends only on `Actor`; infrastructure handles `ApplicationUser`
4. **Testability** — Business logic tests never need to mock `UserManager<ApplicationUser>`

---

## Registration Flow — Atomic Actor ↔ ApplicationUser Creation

**File:** `Features/Auth/Register/RegisterCommandHandler.cs`

The registration handler demonstrates the critical pattern: **create Actor first, then link ApplicationUser**.

```csharp
public sealed class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    EscrowDbContext dbContext)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (request.Password != request.ConfirmPassword)
            return RegisterResult.FailureResult("Passwords do not match.");

        // Step 1: Create Actor (domain entity)
        var actor = new Actor
        {
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
            // WalletAddress remains null until Web3 link (future)
        };

        // Step 2: Wrap in database transaction for atomicity
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Step 3: Persist Actor first
            dbContext.Actors.Add(actor);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Step 4: Create ApplicationUser with ActorId FK bridge
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                ActorId = actor.Id  // ← Bridge: ApplicationUser → Actor
            };

            // Step 5: Use UserManager to hash password and create user
            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                await transaction.CommitAsync(cancellationToken);
                return RegisterResult.SuccessResult();
            }
            else
            {
                // UserManager failed (duplicate email, weak password, etc.)
                // Rollback Actor creation to maintain consistency
                await transaction.RollbackAsync(cancellationToken);
                var errors = string.Join(" ", result.Errors.Select(e => e.Description));
                return RegisterResult.FailureResult(errors);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            return RegisterResult.FailureResult($"Registration failed: {ex.Message}");
        }
    }
}
```

### Transaction Atomicity — CRITICAL

**Why the transaction?**
- If `Actor` creation succeeds but `ApplicationUser` creation fails (duplicate email, weak password, etc.), we'd have an orphaned `Actor` with no authentication.
- The database transaction ensures **both succeed or both fail** — no partial state.

**Sequence:**
1. Begin transaction
2. Create `Actor` → `SaveChangesAsync()` generates `Actor.Id`
3. Create `ApplicationUser` with `ActorId = actor.Id` → `UserManager.CreateAsync()`
4. If `UserManager` succeeds → commit transaction
5. If `UserManager` fails → rollback transaction (deletes Actor)

---

## Database Schema

**Tables Created by Migration:**

```sql
-- Domain table (application-defined)
CREATE TABLE "Actors" (
    "Id" SERIAL PRIMARY KEY,
    "DisplayName" TEXT NOT NULL,
    "WalletAddress" TEXT NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Identity table (ASP.NET Core Identity)
CREATE TABLE "AspNetUsers" (
    "Id" INTEGER PRIMARY KEY,
    "UserName" TEXT NOT NULL,
    "NormalizedUserName" TEXT NULL,
    "Email" TEXT NOT NULL,
    "NormalizedEmail" TEXT NULL,
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMP NULL,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
    
    -- Bridge: FK to Actor
    "ActorId" INTEGER NOT NULL,
    CONSTRAINT "FK_AspNetUsers_Actors_ActorId" 
        FOREIGN KEY ("ActorId") REFERENCES "Actors" ("Id") ON DELETE CASCADE
);

-- Indexes for performance
CREATE UNIQUE INDEX "IX_AspNetUsers_NormalizedUserName" ON "AspNetUsers" ("NormalizedUserName");
CREATE INDEX "IX_AspNetUsers_NormalizedEmail" ON "AspNetUsers" ("NormalizedEmail");
CREATE INDEX "IX_AspNetUsers_ActorId" ON "AspNetUsers" ("ActorId");
```

**Key Constraint:** `ON DELETE CASCADE` ensures that deleting an `Actor` also deletes the linked `ApplicationUser`.

---

## Future Web3 Integration

The hybrid identity pattern enables seamless Web3 integration:

### Phase 1: Web2-Only (Current State ✅)
- User registers with email/password
- `Actor.WalletAddress` is `NULL`
- Authentication via ASP.NET Core Identity

### Phase 2: Web3 Wallet Linking (Future)
- User connects MetaMask/WalletConnect
- Signature verification proves wallet ownership
- `Actor.WalletAddress` set to `0x...` address
- User can log in via email **OR** wallet signature

### Phase 3: Web3-Only (Future)
- User registers by signing a message with their wallet
- `Actor` created with `WalletAddress` populated
- `ApplicationUser` created with empty password (wallet-only auth)
- Authentication via signature verification, not password

**No Breaking Changes Required:**  
The `ActorId` FK bridge remains the integration point. Adding Web3 auth is **extending** the system, not refactoring it.

---

## Security Considerations

### Actor ↔ ApplicationUser Consistency
- **Always create Actor first** in a transaction — `Actor.Id` must exist before `ApplicationUser.ActorId` can reference it
- **Never allow orphaned records** — transaction rollback on failure ensures consistency
- **Cascade deletes** — if an `Actor` is deleted (rare), the linked `ApplicationUser` is also deleted

### Web3-Specific Risks (Future)
- **Wallet ownership verification** — require signature of a server-generated nonce to prove control
- **Replay attack prevention** — nonces must be single-use and expire after 5 minutes
- **Phishing protection** — display wallet address during sign-in flow to prevent phishing
- **Private key loss** — implement social recovery or guardian system (future roadmap)

### Regulatory Compliance
- **PII:** `Actor.DisplayName` and `ApplicationUser.Email` are PII — never log these values
- **GDPR Right to Erasure** — soft-delete `Actor` records; hard-delete `ApplicationUser` after retention period
- **Audit Trail** — all `Actor` changes must emit domain events for compliance traceability

---

## Testing Strategy

### Unit Tests
- **`ActorTests.cs`** — Verify `Actor` model validation and defaults
- **`ApplicationUserTests.cs`** — Verify `ApplicationUser` inherits Identity properties correctly

### Integration Tests
- **`RegisterCommandHandlerTests.cs`** — Verify transaction atomicity:
  - Success: both `Actor` and `ApplicationUser` created
  - Failure: neither `Actor` nor `ApplicationUser` exists after rollback
  - Duplicate email: rollback preserves consistency
- **`EscrowDbContextIdentityTests.cs`** — Verify FK constraint, cascade delete, unique indexes

### Test Coverage (Current)
- ✅ Actor model: 5/5 tests passing
- ✅ ApplicationUser model: 5/5 tests passing
- ✅ RegisterCommandHandler: 7/7 tests passing (transaction atomicity verified)
- ✅ DbContext integration: 5/5 tests passing

---

## Related Documentation

- **Authentication Flow:** See `authentication.md` for login/logout implementation
- **Authorization:** See `../architecture/authorization.md` (future) for policy-based access control
- **Domain Events:** See `../architecture/event-bus.md` for audit trail via domain events
- **EF Core Migrations:** See `../architecture/overview.md` for migration strategy

---

## Code Locations

| Component | File Path |
|---|---|
| Actor model | `Models/Actor.cs` |
| ApplicationUser model | `Models/ApplicationUser.cs` |
| Registration handler | `Features/Auth/Register/RegisterCommandHandler.cs` |
| Registration command | `Features/Auth/Register/RegisterCommand.cs` |
| EF Core configuration | `Data/EscrowDbContext.cs` |
| Identity migration | `Migrations/20260416011350_AddIdentityToEscrowDb.cs` |

---

## Change Log

| Date | Change | Author |
|---|---|---|
| 2026-04-16 | Initial documentation after Track B completion | Gemini Agent |
| 2026-04-16 | Added registration flow with transaction atomicity | Gemini Agent |
| 2026-04-16 | Documented Web3 future integration path | Gemini Agent |

---

## Glossary

- **Actor** — Domain entity representing a user in the business model
- **ApplicationUser** — ASP.NET Core Identity entity managing authentication credentials
- **Bridge Pattern** — Design pattern linking two entities with a foreign key relationship
- **Hybrid Identity** — Supporting both Web2 (email/password) and Web3 (wallet signature) authentication
- **Web2** — Traditional web authentication (email, OAuth, SAML)
- **Web3** — Blockchain-based authentication (wallet signatures, decentralized identity)
- **Atomic Transaction** — Database operation that either fully succeeds or fully fails (no partial state)
