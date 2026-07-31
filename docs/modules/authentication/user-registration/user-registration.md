# User Registration

> Allow new users to create accounts via email/password registration using ASP.NET Core Identity.
> This is the foundational authentication feature that enables users to access the escrow platform.

## Status: ✅ Implemented

---

## Overview

The User Registration feature allows new users to create accounts on the NexTruzt.io platform. It uses **ASP.NET Core Identity** for secure user management with password hashing, email uniqueness validation, and claims-based identity.

This is implemented as a **MediatR CQRS vertical slice** with a Blazor Server component providing the UI.

## User Stories

Stories for the email/password registration flow that creates an ApplicationUser via UserManager. The flow currently does not require email confirmation in MVP.

### Story 1 — Self-service registration

**As a** Client or Consultant, **I want** to register with my email, a strong password, and a display name, **so that** I can begin participating in secure payment holding without manual onboarding.

**Acceptance Criteria:**

- [ ] a new ApplicationUser is created with my email as UserName
- [ ] RegisterResult.Success is true
- [ ] I am redirected to the post-registration landing page

```gherkin
Feature: New account creation
  Scenario: Valid registration succeeds
    Given I am an unauthenticated visitor
    When I submit an email, matching password and confirmation, and display name
    Then a new ApplicationUser is created with my email as UserName
    And RegisterResult.Success is true
    And I am redirected to the post-registration landing page
```

### Story 2 — Password confirmation enforced

**As a** Client, **I want** the form to reject mismatched password and confirmation values before any account is created, **so that** I do not lock myself out by typing the wrong password twice.

**Acceptance Criteria:**

- [ ] RegisterResult.Success is false
- [ ] ErrorMessage is "Passwords do not match."
- [ ] no ApplicationUser is created

```gherkin
Feature: Password match check
  Scenario: Passwords do not match
    Given I enter "P@ssw0rd!" in Password and "p@ssw0rd!" in ConfirmPassword
    When I submit the form
    Then RegisterResult.Success is false
    And ErrorMessage is "Passwords do not match."
    And no ApplicationUser is created
```

### Story 3 — Email uniqueness

**As a** Platform Admin, **I want** registration to fail if the email is already in use, **so that** every Actor maps 1:1 to an email-provider IdentityMapping and counterparty notifications cannot collide.

**Acceptance Criteria:**

- [ ] UserManager.CreateAsync returns a DuplicateUserName error
- [ ] RegisterResult.ErrorMessage surfaces "Username 'bob@example.com' is already taken."

```gherkin
Feature: Unique email per account
  Scenario: Email already registered
    Given an ApplicationUser exists with email "bob@example.com"
    When a second registration is submitted for "bob@example.com"
    Then UserManager.CreateAsync returns a DuplicateUserName error
    And RegisterResult.ErrorMessage surfaces "Username 'bob@example.com' is already taken."
```


## MediatR Command

```csharp
// File: Features/Auth/Register/RegisterCommand.cs
public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string DisplayName
) : IRequest<RegisterResult>;
```

## Result DTO

```csharp
// File: Features/Auth/Register/RegisterCommand.cs
public sealed record RegisterResult(bool Success, string? ErrorMessage = null)
{
    public static RegisterResult SuccessResult() => new(true);
    public static RegisterResult FailureResult(string message) => new(false, message);
}
```

## Handler Flow

```csharp
// File: Features/Auth/Register/RegisterCommandHandler.cs
public sealed class RegisterCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<RegisterCommand, RegisterResult>
```

### Processing Steps

1. **Password Validation** — Check if `Password` matches `ConfirmPassword`
2. **User Creation** — Create new `ApplicationUser` with `Email` and `UserName` set to email
3. **Identity Processing** — Delegate to `UserManager.CreateAsync()` for password hashing and validation
4. **Result Processing** — Return success or aggregate Identity validation errors

### Validation Rules

| Rule | Enforced By | Behavior |
|------|------------|----------|
| **Password Match** | Handler | Returns "Passwords do not match." if mismatch |
| **Email Format** | ASP.NET Identity | Returns Identity validation error |
| **Email Uniqueness** | ASP.NET Identity | Returns "Username 'email' is already taken." |
| **Password Strength** | ASP.NET Identity | Returns password policy violations |
| **Required Fields** | ASP.NET Identity | Returns field-specific validation errors |

## UI Component

### Blazor Server Page

**File:** `Components/Pages/Auth/Register.razor`

**Route:** `/auth/register`

**Features:**
- Responsive Bootstrap 5 form design
- Real-time validation via `DataAnnotationsValidator`
- Loading state with spinner during submission
- Localized strings via `IStringLocalizer<SharedResource>`
- Error display with dismissible alerts
- Redirect to login page on successful registration

### Form Model

```csharp
private sealed class RegisterFormModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### Code-Behind Logic

**File:** `Components/Pages/Auth/Register.razor.cs`

**Dependencies:**
- `IStringLocalizer<SharedResource>` — Localization
- `NavigationManager` — Post-registration redirect
- `IMediator` — Command dispatch

**Flow:**
1. User fills form and clicks Submit
2. `HandleRegister()` creates `RegisterCommand` from form data
3. Command sent via `IMediator.Send()`
4. Success → Redirect to `/auth/login`
5. Failure → Display error message

## Authentication Integration

### Identity Configuration

The Register feature integrates with ASP.NET Core Identity configured in `Program.cs`:

```csharp
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Password requirements configured here
    // Email confirmation settings configured here
})
.AddEntityFrameworkStores<EscrowDbContext>();
```

### Database Schema

Uses standard ASP.NET Core Identity tables:
- **AspNetUsers** — User accounts (email, hashed password, etc.)
- **AspNetUserClaims** — User claims
- **AspNetRoles**, **AspNetUserRoles** — Role-based authorization

**ApplicationUser** extends `IdentityUser<int>` to integrate with the Actor model for escrow transactions.

## Localization

### Resource Keys

| Key | en-US | es-MX |
|-----|-------|-------|
| `RegisterTitle` | "Create Account" | "Crear Cuenta" |
| `Email` | "Email" | "Correo Electrónico" |
| `Password` | "Password" | "Contraseña" |
| `ConfirmPassword` | "Confirm Password" | "Confirmar Contraseña" |
| `Register` | "Create Account" | "Crear Cuenta" |
| `Registering` | "Creating..." | "Creando..." |
| `HaveAccount` | "Already have an account?" | "¿Ya tienes una cuenta?" |
| `SignIn` | "Sign In" | "Iniciar Sesión" |
| `Description` | "Full Name" | "Nombre Completo" |

## Security Considerations

### Password Security
- **Hashing:** ASP.NET Identity uses secure password hashing (PBKDF2)
- **Validation:** Enforces password complexity requirements
- **No Plaintext Storage:** Passwords are never stored in plaintext

### Input Validation
- **Email Format:** Validated by Identity framework
- **SQL Injection:** Protected by EF Core parameterized queries
- **XSS:** Blazor provides automatic HTML encoding

### Regulatory Compliance
- **User-Facing Copy:** Uses "Create Account" instead of "escrow registration" (regulatory requirement)
- **Data Collection:** Minimal data collection (email, name) for fintech compliance
- **Audit Trail:** Registration events can be logged for compliance audit

## Testing

### Unit Tests

**File:** `EscrowApp.Tests/Features/Auth/Register/RegisterCommandHandlerTests.cs`

**Coverage:**
- ✅ **Happy Path:** Valid registration succeeds
- ✅ **Password Mismatch:** Returns appropriate error
- ✅ **UserManager Failures:** Handles Identity validation errors
- ✅ **Exception Handling:** Propagates unexpected exceptions
- ✅ **Edge Cases:** Empty passwords, duplicate emails

**Test Framework:** xUnit + Moq + FluentAssertions

### Integration Tests

**Recommended:** WebApplicationFactory tests for end-to-end registration flow (not yet implemented)

## Error Handling

### Common Errors

| Error Source | Error Message | User Experience |
|--------------|---------------|-----------------|
| **Password Mismatch** | "Passwords do not match." | Form validation error |
| **Duplicate Email** | "Username 'email' is already taken." | Form validation error |
| **Weak Password** | "Passwords must be at least 6 characters." | Form validation error |
| **Invalid Email** | "Invalid email format." | Form validation error |
| **System Error** | "An unexpected error occurred." | Generic fallback message |

### Error Display
- **Client-Side:** Real-time validation via Blazor validation components
- **Server-Side:** Error messages returned from RegisterResult and displayed in alert banner
- **Dismissible:** Users can dismiss error messages via close button

## Future Enhancements

### Post-MVP Features
1. **Email Confirmation** — Require email verification before account activation
2. **Social Login** — Google, Microsoft, GitHub OAuth integration
3. **Entra ID Integration** — Enterprise SSO for organizational accounts
4. **Profile Pictures** — Avatar upload and management
5. **Account Recovery** — Forgot password flow via email

### Performance Optimizations
1. **Async Email Sending** — Background email confirmation
2. **Rate Limiting** — Prevent registration abuse
3. **Captcha Integration** — Bot protection

## Related Documentation

- **Authentication Overview:** `docs/cross-cutting/authentication/aspnet-identity-mvp.md`
- **Hybrid Identity:** `docs/cross-cutting/hybrid-identity/hybrid-identity.md`
- **Localization:** `docs/cross-cutting/localization/localization.md`
- **Testing Strategy:** `docs/cross-cutting/testing/testing-strategy.md`

---

**Last Updated:** 2026-04-16  
**Implemented By:** AI Assistant  
**Status:** Production Ready