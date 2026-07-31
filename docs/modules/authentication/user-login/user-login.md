# User Login

> Allow existing users to authenticate with email/password credentials using ASP.NET Core Identity.
> This enables secure access to the escrow platform and establishes user sessions.

## Status: ✅ Implemented

---

## Overview

The User Login feature enables existing users to authenticate with their email and password credentials to access the NexTruzt.io platform. It uses **ASP.NET Core Identity SignInManager** for secure credential validation with built-in features like account lockout protection and "Remember Me" functionality.

This is implemented as a **MediatR CQRS vertical slice** with a Blazor Server component providing the UI.

## User Stories

User stories for the email/password sign-in flow built on SignInManager. Compliance-sensitive: error messages must avoid leaking which factor (email vs password) was incorrect.

### Story 1 — Returning user signs in

**As a** Client or Consultant, **I want** to sign in with my email and password, **so that** I can access my dashboard and act on transactions where I am a participant.

**Acceptance Criteria:**

- [ ] SignInManager.PasswordSignInAsync returns Succeeded=true
- [ ] an authentication cookie is issued
- [ ] I am redirected to my role-appropriate dashboard

```gherkin
Feature: Sign-in with valid credentials
  Scenario: Successful sign-in
    Given I have a confirmed account with email "alice@example.com"
    When I submit valid credentials on the login page
    Then SignInManager.PasswordSignInAsync returns Succeeded=true
    And an authentication cookie is issued
    And I am redirected to my role-appropriate dashboard
```

### Story 2 — Lockout after repeated failures

**As a** Platform Admin, **I want** brute-force sign-in attempts to trigger account lockout, **so that** credential-stuffing attacks against the platform fail loudly and leave an audit trail.

**Acceptance Criteria:**

- [ ] the next attempt returns SignInResult.IsLockedOut = true
- [ ] the user-facing message is "Account is locked due to too many failed login attempts. Try again later."
- [ ] the underlying reason is recorded in structured logs (without leaking PII)

```gherkin
Feature: Lockout protection
  Scenario: Lockout after configured failed attempts
    Given the lockout threshold is 5 failed attempts
    When a user fails sign-in 5 times in a row
    Then the next attempt returns SignInResult.IsLockedOut = true
    And the user-facing message is "Account is locked due to too many failed login attempts. Try again later."
    And the underlying reason is recorded in structured logs (without leaking PII)
```

### Story 3 — Remember-me persistence

**As a** Client, **I want** the option to stay signed in on my own device, **so that** I do not have to re-enter my password each visit while still being able to sign out anywhere.

**Acceptance Criteria:**

- [ ] a persistent authentication cookie is issued
- [ ] closing and reopening the browser keeps me signed in until cookie expiry
- [ ] a session cookie is issued
- [ ] closing the browser ends the session

```gherkin
Feature: Persistent sign-in
  Scenario: Remember-me selected
    Given I check "Remember me" during sign-in
    When sign-in succeeds
    Then a persistent authentication cookie is issued
    And closing and reopening the browser keeps me signed in until cookie expiry

  Scenario: Remember-me not selected
    Given I do not check "Remember me"
    When sign-in succeeds
    Then a session cookie is issued
    And closing the browser ends the session
```

### Story 4 — Generic error to prevent enumeration

**As a** Compliance Officer, **I want** invalid sign-ins to return a generic message regardless of whether the email exists, **so that** attackers cannot enumerate valid accounts via differential responses.

**Acceptance Criteria:**

- [ ] the response is "Invalid email or password."
- [ ] the response is also "Invalid email or password."

```gherkin
Feature: Non-enumerable login errors
  Scenario: Unknown email
    When sign-in is attempted with an email that does not exist
    Then the response is "Invalid email or password."

  Scenario: Wrong password for known email
    When sign-in is attempted with a valid email and wrong password
    Then the response is also "Invalid email or password."
```


## MediatR Command

```csharp
// File: Features/Auth/Login/LoginCommand.cs
public sealed record LoginCommand(
    string Email,
    string Password,
    bool RememberMe = false
) : IRequest<LoginResult>;
```

## Result DTO

```csharp
// File: Features/Auth/Login/LoginCommand.cs
public sealed record LoginResult(bool Success, string? ErrorMessage = null)
{
    public static LoginResult SuccessResult() => new(true);
    public static LoginResult FailureResult(string message) => new(false, message);
}
```

## Handler Flow

```csharp
// File: Features/Auth/Login/LoginCommandHandler.cs
public sealed class LoginCommandHandler(SignInManager<ApplicationUser> signInManager)
    : IRequestHandler<LoginCommand, LoginResult>
```

### Processing Steps

1. **Credential Validation** — Call `SignInManager.PasswordSignInAsync()` with email/password
2. **Lockout Protection** — Enable `lockoutOnFailure: true` for brute-force protection
3. **Session Persistence** — Respect `RememberMe` flag for persistent authentication cookies
4. **Result Processing** — Return appropriate success/failure result with specific error messages

### Authentication Scenarios

| Scenario | SignInResult Property | LoginResult Response |
|----------|----------------------|----------------------|
| **Valid Credentials** | `Succeeded = true` | `Success = true` |
| **Invalid Credentials** | `Succeeded = false` | "Invalid email or password." |
| **Account Locked** | `IsLockedOut = true` | "Account is locked due to too many failed login attempts. Try again later." |
| **2FA Required** | `RequiresTwoFactor = true` | "Two-factor authentication required." |

## UI Component

### Blazor Server Page

**File:** `Components/Pages/Auth/Login.razor`

**Route:** `/auth/login`

**Features:**
- Clean, responsive Bootstrap 5 form design
- Real-time validation via `DataAnnotationsValidator`
- Loading state with spinner during authentication
- Localized strings via `IStringLocalizer<SharedResource>`
- Error display with dismissible alerts
- "Remember Me" checkbox for persistent sessions
- Link to registration page for new users

### Form Model

```csharp
private sealed class LoginFormModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
```

### Code-Behind Logic

**File:** `Components/Pages/Auth/Login.razor.cs`

**Dependencies:**
- `IStringLocalizer<SharedResource>` — Localization
- `NavigationManager` — Post-login redirect
- `IMediator` — Command dispatch

**Flow:**
1. User fills credentials and clicks Sign In
2. `HandleLogin()` creates `LoginCommand` from form data
3. Command sent via `IMediator.Send()`
4. Success → Redirect to `/` (dashboard)
5. Failure → Display specific error message

## Security Features

### Brute-Force Protection
- **Account Lockout:** Enabled via `lockoutOnFailure: true` in SignInManager
- **Failed Attempt Tracking:** ASP.NET Identity tracks failed login attempts
- **Lockout Duration:** Configurable in Identity options (default: 5 minutes)

### Session Management
- **Cookie Authentication:** Uses ASP.NET Core cookie authentication
- **Persistent Sessions:** "Remember Me" extends cookie lifetime
- **Secure Cookies:** HTTPS-only in production, SameSite protection

### Password Security
- **No Plaintext Storage:** Passwords are never stored in plaintext
- **Hash Verification:** SignInManager verifies against stored password hash
- **Timing Attack Protection:** Constant-time comparison operations

## Authentication Integration

### Identity Configuration

The Login feature integrates with ASP.NET Core Identity configured in `Program.cs`:

```csharp
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // SignIn settings  
    options.SignIn.RequireConfirmedAccount = false; // MVP: allow unconfirmed emails
})
.AddEntityFrameworkStores<EscrowDbContext>();
```

### Cookie Configuration

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/auth/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Remember Me duration
    options.SlidingExpiration = true;
});
```

## Localization

### Resource Keys

| Key | en-US | es-MX |
|-----|-------|-------|
| `LoginTitle` | "Sign In" | "Iniciar Sesión" |
| `Email` | "Email" | "Correo Electrónico" |
| `Password` | "Password" | "Contraseña" |
| `RememberMe` | "Remember Me" | "Recordarme" |
| `SignIn` | "Sign In" | "Iniciar Sesión" |
| `SigningIn` | "Signing In..." | "Iniciando Sesión..." |
| `NoAccount` | "Don't have an account?" | "¿No tienes una cuenta?" |
| `CreateAccount` | "Create Account" | "Crear Cuenta" |

## Error Handling

### Authentication Errors

| Error Condition | User-Friendly Message | Security Note |
|----------------|----------------------|---------------|
| **Invalid Email/Password** | "Invalid email or password." | Generic message prevents username enumeration |
| **Account Locked** | "Account is locked due to too many failed login attempts. Try again later." | Clear guidance on resolution |
| **2FA Required** | "Two-factor authentication required." | Future enhancement support |
| **System Error** | "An unexpected error occurred. Please try again." | Fallback for unexpected exceptions |

### Error Display
- **Client-Side:** Real-time validation via Blazor validation components
- **Server-Side:** Authentication errors returned from LoginResult and displayed in alert banner
- **Dismissible:** Users can dismiss error messages via close button
- **Accessible:** Error messages include ARIA attributes for screen readers

## Testing

### Unit Tests

**File:** `EscrowApp.Tests/Features/Auth/Login/LoginCommandTests.cs`

**Coverage:**
- ✅ **Successful Login:** Valid credentials return success
- ✅ **Invalid Credentials:** Failed authentication returns appropriate error
- ✅ **Account Lockout:** Locked account returns lockout message
- ✅ **2FA Required:** Multi-factor authentication scenario
- ✅ **Exception Handling:** SignInManager exceptions are handled gracefully

**Test Framework:** xUnit + Moq + FluentAssertions

### Integration Tests

**Recommended:** WebApplicationFactory tests for end-to-end authentication flow (not yet implemented)

## Performance Considerations

### Password Verification
- **Efficient Hashing:** ASP.NET Identity uses optimized password verification
- **Database Queries:** Single query to retrieve user by email
- **Memory Usage:** SignInManager handles authentication state efficiently

### Session Management
- **Cookie Size:** Minimal authentication cookie payload
- **Database Load:** Authentication state stored in cookies, not database
- **Caching:** User claims cached in authentication cookie

## Monitoring & Audit

### Security Events

| Event | Log Level | Data Logged |
|-------|-----------|-------------|
| **Successful Login** | Information | Email (hashed), IP, timestamp |
| **Failed Login** | Warning | Email (hashed), IP, failure reason, timestamp |
| **Account Lockout** | Warning | Email (hashed), IP, lockout duration |
| **Unlocked Account** | Information | Email (hashed), unlock timestamp |

### Compliance
- **Audit Trail:** Login attempts logged for security monitoring
- **Data Protection:** User emails are hashed in logs to protect PII
- **Retention:** Authentication logs follow data retention policies

## Future Enhancements

### Post-MVP Features
1. **Multi-Factor Authentication** — SMS, authenticator app, email codes
2. **Social Login** — Google, Microsoft, GitHub OAuth integration
3. **Single Sign-On** — Entra ID integration for enterprise customers
4. **Passwordless Login** — Magic links, WebAuthn biometric authentication
5. **Login Analytics** — Success rates, geographic patterns, device tracking

### Security Hardening
1. **CAPTCHA Integration** — Bot protection for repeated failed attempts
2. **Device Fingerprinting** — Detect unusual login patterns
3. **IP Allowlisting** — Restrict access by geographic region or IP range
4. **Advanced Threat Detection** — ML-based anomaly detection

## Related Documentation

- **Authentication Overview:** `docs/cross-cutting/authentication/aspnet-identity-mvp.md`
- **User Registration:** `docs/features/user-registration/user-registration.md`
- **Hybrid Identity:** `docs/cross-cutting/hybrid-identity/hybrid-identity.md`
- **Security Audit:** `docs/audits/security-audit/security-audit.md`
- **Localization:** `docs/cross-cutting/localization/localization.md`

---

**Last Updated:** 2026-04-16  
**Implemented By:** AI Assistant  
**Status:** Production Ready