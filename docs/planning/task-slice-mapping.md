# Task ↔ Slice Mapping — NexTruzt.io EscrowApp

**Purpose:** Reconcile the two numbering systems used across planning documents.

---

## Unified Mapping Table

| Task # | Slice # | Feature | Status | File References |
|--------|---------|---------|--------|-----------------|
| **Task 1** | **Slice 1** | ApplicationUser Model | ✅ COMPLETE | `Models/ApplicationUser.cs` |
| **Task 2** | **Slice 2** | Identity DbContext + NuGet | ✅ COMPLETE | `EscrowDbContext.cs` + packages |
| **Task 3** | **Slice 3** | EF Core Migration | ✅ COMPLETE | `Migrations/AddIdentityToEscrowDb.cs` |
| **Task 4** | **Slice 4** | DI Registration | ✅ COMPLETE | `Program.cs` Identity services |
| **—** | **Slice 5** | **Blazor Auth Config** | ✅ **COMPLETE** | `Routes.razor` + `RevalidatingIdentityAuthenticationStateProvider` |
| **Task 5** | **Slice 6** | **Login Page** | ✅ **COMPLETE** | `Login.razor` + `LoginCommand/Handler` |
| **Task 6** | **Slice 7** | Register Page | 📋 PENDING | `Register.razor` (stub only) |
| **Task 7** | **Slice 8** | Logout Functionality | 📋 PENDING | `NavBar.razor` (missing logout) |
| **Task 8** | **Slice 10** | **Dashboard Auth Guard** | ✅ **COMPLETE** | `[Authorize]` + `Unauthorized.razor` |
| **Task 9** | **Slice 9** | Auth UI Localization | 🟡 PARTIAL | `SharedResource.resx` (Spanish missing) |

---

## Resolution Rules

### ✅ **COMPLETED Slices: 6/10 (60%)**
- Slices 1-6: ✅ Foundation + Login flow complete
- Slice 10: ✅ Dashboard authorization complete

### 📋 **PENDING Slices: 4/10 (40%)**
- Slice 7: Register Page (Task 6)
- Slice 8: Logout (Task 7)  
- Slice 9: Spanish localization (Task 9)
- Slices 11-14: Testing & documentation

### 🔧 **Numbering Conflicts Resolved:**
1. **"Slice 5" = Blazor Auth Config** (not Login Page)
2. **"Task 5" = Login Page** (which is Slice 6)
3. **Both systems valid** — use consistently within each doc

---

## Documentation Standards Going Forward

| Document | Numbering System | Usage |
|----------|------------------|-------|
| `task-checklist.md` | **Task-based** (Task 1-14) | Detailed implementation tracking |
| `implementation-plan.md` | **Slice-based** (Slice 1-14) | High-level phase planning |
| `task-slice-mapping.md` | **Both** | Cross-reference / Rosetta Stone |

**Rule:** When referencing across documents, always specify both: `"Task 5 / Slice 6 (Login Page)"`

---

## Current Status Summary

**Phase 1: Identity Infrastructure** → ✅ **100% COMPLETE** (Slices 1-4)
**Phase 2: Blazor Authentication** → 🔄 **60% COMPLETE** (4/6 slices)
- ✅ Slice 5: Blazor auth config 
- ✅ Slice 6: Login page
- 📋 Slice 7: Register page  
- 📋 Slice 8: Logout functionality
- ✅ Slice 10: Dashboard guards
- 🟡 Slice 9: Localization (partial)

**Next Immediate Work:** Slice 7 (Register Page) → Slice 8 (Logout) → Slice 9 (Spanish translations)