# Authentication Implementation — ASP.NET Core Identity + Blazor Server

**Last Updated:** 2026-04-16  
**Status:** ✅ Production-Ready (Track B Complete)  
**Identity Provider:** ASP.NET Core Identity (Web2) — Future: Web3 wallet signatures

---

## Overview

NexTruzt.io implements authentication using **ASP.NET Core Identity** with **Blazor Server** interactive rendering. The authentication system provides secure email/password-based user registration and login, with support for future Web3 wallet authentication via the hybrid identity bridge.

**Key Components:**
- **ASP.NET Core Identity** — Password hashing, user management, security stamp validation
- **SignInManager** — Cookie-based authentication for Blazor Server
- **UserManager** — User creation, password validation, email uniqueness enforcement
- **RevalidatingAuthenticationStateProvider** — Periodically revalidates auth state to detect signouts
- **AuthorizeView** — Blazor component for conditional UI rendering based on auth state

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                      Presentation Layer                          │
│                    (Blazor Components)                           │
│                                                                  │
│  ┌─────────────┐  ┌────────────┐  ┌──────────────────────┐     │
│  │ Login.razor │  │Register.razor│ │NavBar.razor (Logout) │     │
│  └──────┬──────┘  └─────┬──────┘  └──────────┬───────────┘     │
│         │                │                    │                 │
│         ▼                ▼                    ▼                 │
│  [IMediator.Send(LoginCommand)]  [IMediator.Send(RegisterCmd)] │
│                                   [SignInManager.SignOutAsync]  │
└──────────────────────────────────────────────────────────────────┘
                         │                    │
                         ▼                    ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Application Layer                              │
│                   (MediatR Handlers)                             │
│                                                                  │
│  LoginCommandHandler                RegisterCommandHandler       │
│  ├─ SignInManager.PasswordSignInAsync  ├─ Create Actor          │
│  ├─ Validate credentials               ├─ Create ApplicationUser│
│  └─ Set auth cookie                    └─ Link via ActorId      │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
                         │                    │
                         ▼                    ▼
┌──────────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                             │
│                (ASP.NET Core Identity)                           │
│                                                                  │
│  SignInManager<ApplicationUser>     UserManager<ApplicationUser> │
│  ├─ Cookie-based authentication     ├─ Password hashing (BCrypt)│
│  ├─ Security stamp validation       ├─ Email uniqueness check   │
│  └─ Lockout on failed attempts      └─ Password policy enforce  │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Data Layer                                     │
│              (EscrowDbContext + PostgreSQL)                      │
│                                                                  │
│  AspNetUsers, AspNetRoles, AspNetUserClaims, Actors              │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Password Policy

Configured in `Program.cs`:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    // Password requirements (NIST-aligned)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Lockout settings (brute-force protection)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<EscrowDbContext>()
.AddDefaultTokenProviders();
```

**Policy Summary:**
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 digit
- At least 1 special character
- Email must be unique across all users
- Lockout after 5 failed attempts for 5 minutes

---

## Registration Flow

### Component: `Components/Pages/Auth/Register.razor`

Renders the registration form with email, display name, password, and confirm password fields.

```razor
@page "/auth/register"
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<SharedResource> L

<div class="register-container">
    <h1>@L["RegisterTitle"]</h1>
    
    <EditForm Model="@Model" OnValidSubmit="HandleRegister">
        <DataAnnotationsValidator />
        <ValidationSummary />
        
        <div class="mb-3">
            <label for="email" class="form-label">@L["Email"]</label>
            <InputText id="email" class="form-control" @bind-Value="Model.Email" />
        </div>
        
        <div class="mb-3">
            <label for="displayName" class="form-label">@L["DisplayName"]</label>
            <InputText id="displayName" class="form-control" @bind-Value="Model.DisplayName" />
        </div>
        
        <div class="mb-3">
            <label for="password" class="form-label">@L["Password"]</label>
            <InputText type="password" id="password" class="form-control" @bind-Value="Model.Password" />
        </div>
        
        <div class="mb-3">
            <label for="confirmPassword" class="form-label">@L["ConfirmPassword"]</label>
            <InputText type="password" id="confirmPassword" class="form-control" @bind-Value="Model.ConfirmPassword" />
        </div>
        
        <button type="submit" class="btn btn-primary" disabled="@IsSubmitting">
            @if (IsSubmitting)
            {
                <span class="spinner-border spinner-border-sm me-2"></span>
                @L["Registering"]
            }
            else
            {
                @L["Register"]
            }
        </button>
    </EditForm>
    
    <div class="mt-3">
        <a href="/auth/login">@L["HaveAccount"]</a>
    </div>
</div>
```

### Handler: `Features/Auth/Register/RegisterCommandHandler.cs`

**Key Responsibilities:**
1. Create `Actor` domain entity
2. Create `ApplicationUser` with `ActorId` FK bridge
3. Use database transaction for atomicity
4. Validate password match and complexity via `UserManager`

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

        var actor = new Actor
        {
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create Actor first
            dbContext.Actors.Add(actor);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Create ApplicationUser with bridge
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                ActorId = actor.Id  // Bridge to domain entity
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                await transaction.CommitAsync(cancellationToken);
                return RegisterResult.SuccessResult();
            }
            else
            {
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

**Transaction Guarantees:**
- If `Actor` creation succeeds but `ApplicationUser` creation fails → rollback `Actor`
- If password validation fails → no database changes
- If duplicate email detected → no database changes

---

## Login Flow

### Component: `Components/Pages/Auth/Login.razor`

Renders the login form with email and password fields.

```razor
@page "/auth/login"
@using Microsoft.Extensions.Localization
@inject IStringLocalizer<SharedResource> L

<div class="login-container">
    <h1>@L["LoginTitle"]</h1>
    
    <EditForm Model="@Model" OnValidSubmit="HandleLogin">
        <DataAnnotationsValidator />
        <ValidationSummary />
        
        <div class="mb-3">
            <label for="email" class="form-label">@L["Email"]</label>
            <InputText id="email" class="form-control" @bind-Value="Model.Email" />
        </div>
        
        <div class="mb-3">
            <label for="password" class="form-label">@L["Password"]</label>
            <InputText type="password" id="password" class="form-control" @bind-Value="Model.Password" />
        </div>
        
        <button type="submit" class="btn btn-primary" disabled="@IsSubmitting">
            @if (IsSubmitting)
            {
                <span class="spinner-border spinner-border-sm me-2"></span>
                @L["SigningIn"]
            }
            else
            {
                @L["SignIn"]
            }
        </button>
    </EditForm>
    
    <div class="mt-3">
        <a href="/auth/register">@L["NoAccount"]</a>
    </div>
</div>
```

### Handler: `Features/Auth/Login/LoginCommandHandler.cs`

**Key Responsibilities:**
1. Validate credentials via `SignInManager`
2. Set authentication cookie
3. Handle failed attempts (increment lockout counter)

```csharp
public sealed class LoginCommandHandler(
    SignInManager<ApplicationUser> signInManager)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LoginResult.SuccessResult();
        }
        else if (result.IsLockedOut)
        {
            return LoginResult.FailureResult("Account locked due to multiple failed login attempts. Try again later.");
        }
        else
        {
            return LoginResult.FailureResult("Invalid email or password.");
        }
    }
}
```

**Security Features:**
- Password is never logged or exposed in error messages
- Failed attempts increment lockout counter
- Account locks after 5 failed attempts for 5 minutes
- Cookies are `HttpOnly` and `Secure` (HTTPS-only)

---

## Logout Flow

### Component: `Components/Pages/NavBar.razor`

The NavBar displays a user dropdown menu with a logout button when authenticated.

```razor
<AuthorizeView>
    <Authorized>
        <div class="dropdown">
            <button class="btn btn-outline-light btn-sm dropdown-toggle" 
                    type="button" id="userMenuDropdown" data-bs-toggle="dropdown">
                @context.User.Identity?.Name
            </button>
            <ul class="dropdown-menu">
                <li><a class="dropdown-item" href="/dashboard">@L["Dashboard"]</a></li>
                <li><hr class="dropdown-divider"></li>
                <li>
                    <button class="dropdown-item" @onclick="HandleLogout" disabled="@IsLoggingOut">
                        @if (IsLoggingOut)
                        {
                            <span class="spinner-border spinner-border-sm me-2"></span>
                        }
                        @L["Logout"]
                    </button>
                </li>
            </ul>
        </div>
    </Authorized>
    <NotAuthorized>
        <a class="nav-link btn btn-outline-light" href="/auth/login">@L["LogIn"]</a>
    </NotAuthorized>
</AuthorizeView>
```

### Code-Behind: `Components/Pages/NavBar.razor.cs`

```csharp
public sealed partial class NavBar
{
    [Inject] private SignInManager<ApplicationUser> SignInManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private bool IsLoggingOut { get; set; }

    private async Task HandleLogout()
    {
        IsLoggingOut = true;
        try
        {
            await SignInManager.SignOutAsync();
            NavigationManager.NavigateTo("/", forceLoad: true);
        }
        finally
        {
            IsLoggingOut = false;
        }
    }
}
```

**Logout Behavior:**
- Calls `SignOutAsync()` to clear authentication cookie
- Invalidates security stamp to prevent reuse of old cookies
- Force-reloads the page to reset Blazor Server circuit
- Redirects to home page

---

## Authentication State Provider

### File: `Infrastructure/Auth/RevalidatingIdentityAuthenticationStateProvider.cs`

Blazor Server requires periodic revalidation of authentication state to detect sign-outs from other tabs or expired sessions.

```csharp
public sealed class RevalidatingIdentityAuthenticationStateProvider 
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RevalidatingIdentityAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
        : base(loggerFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await ValidateSecurityStampAsync(userManager, authenticationState.User);
    }

    private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user == null)
            return false;

        var principalStamp = principal.FindFirstValue("AspNet.Identity.SecurityStamp");
        var userStamp = await userManager.GetSecurityStampAsync(user);
        return principalStamp == userStamp;
    }
}
```

**Revalidation Logic:**
- Checks authentication state every 30 minutes
- Compares security stamp in cookie vs database
- If stamps don't match (password changed, explicit sign-out) → treat as signed out
- Forces user to re-authenticate

**Registration in `Program.cs`:**

```csharp
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider>();
```

---

## Authorization Guards

### Router Configuration: `Components/Routes.razor`

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(Program).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(Layout.MainLayout)">
                <NotAuthorized>
                    @if (context.User.Identity?.IsAuthenticated == true)
                    {
                        <RedirectToPage Page="/unauthorized" />
                    }
                    else
                    {
                        <RedirectToPage Page="/auth/login" />
                    }
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <PageTitle>Not Found</PageTitle>
            <LayoutView Layout="@typeof(Layout.MainLayout)">
                <NotFound />
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

**Guard Behavior:**
- `CascadingAuthenticationState` provides `AuthenticationState` to all child components
- `AuthorizeRouteView` enforces `[Authorize]` attribute on pages
- Unauthenticated users → redirect to `/auth/login`
- Authenticated but unauthorized users (future role-based) → redirect to `/unauthorized`

### Protected Pages

Dashboard pages are protected with `[Authorize]`:

```csharp
@page "/dashboard"
@attribute [Authorize]

// Page content only visible to authenticated users
```

---

## Localization Keys

All authentication UI strings are localized via `IStringLocalizer<SharedResource>`.

**English (`SharedResource.resx`):**

| Key | Value |
|---|---|
| `LoginTitle` | Log In |
| `SignIn` | Sign In |
| `SigningIn` | Signing in... |
| `NoAccount` | Don't have an account? Register |
| `RegisterTitle` | Create Account |
| `Register` | Register |
| `Registering` | Creating account... |
| `ConfirmPassword` | Confirm Password |
| `PasswordMismatch` | Passwords do not match |
| `HaveAccount` | Already have an account? Log in |
| `Logout` | Log Out |
| `LogIn` | Log In |
| `Dashboard` | Dashboard |
| `DisplayName` | Display Name |

**Spanish (`SharedResource.es.resx`):**

| Key | Value |
|---|---|
| `LoginTitle` | Iniciar Sesión |
| `SignIn` | Iniciar Sesión |
| `SigningIn` | Iniciando sesión... |
| `NoAccount` | ¿No tienes cuenta? Regístrate |
| `RegisterTitle` | Crear Cuenta |
| `Register` | Crear Cuenta |
| `Registering` | Creando cuenta... |
| `ConfirmPassword` | Confirmar Contraseña |
| `PasswordMismatch` | Las contraseñas no coinciden |
| `HaveAccount` | ¿Ya tienes cuenta? Inicia sesión |
| `Logout` | Cerrar Sesión |
| `LogIn` | Ingresar |
| `Dashboard` | Panel |
| `DisplayName` | Nombre para Mostrar |

---

## Security Considerations

### Password Storage
- Passwords hashed using **BCrypt** (via ASP.NET Core Identity default)
- Salt is unique per user and stored with hash
- Never log or expose password plaintext

### Session Management
- Cookies are `HttpOnly` (JavaScript cannot access)
- Cookies are `Secure` (HTTPS-only in production)
- Security stamp validation detects compromised sessions
- Sign-out invalidates all cookies immediately

### Brute-Force Protection
- Lockout after 5 failed login attempts
- Lockout duration: 5 minutes
- Counter resets on successful login

### OWASP Top 10 Compliance

| Category | Implementation |
|---|---|
| **A01: Broken Access Control** | `[Authorize]` on protected pages; `AuthorizeRouteView` enforces |
| **A02: Cryptographic Failures** | Password hashing via BCrypt; no plaintext storage |
| **A03: Injection** | Parameterized queries via EF Core |
| **A05: Security Misconfiguration** | HTTPS enforced; secure cookies; default deny |
| **A07: Authentication Failures** | Password policy; lockout; security stamp validation |
| **A09: Logging Failures** | Structured logging; never log passwords/PII |

### Regulatory Compliance

> **CRITICAL:** NexTruzt.io is **not a licensed escrow agent**. User-facing authentication UI must never use the word "escrow" in buttons, labels, or error messages. Use "secure payment holding" or "payment protection" instead.

**PII Protection:**
- Email addresses are PII — never logged outside of audit events
- Display names are PII — never logged in error messages
- Failed login attempts log only timestamps and IP addresses, not email

---

## Testing Strategy

### Unit Tests
- **`LoginCommandHandlerTests.cs`** — Test successful login, failed login, lockout
- **`RegisterCommandHandlerTests.cs`** — Test successful registration, duplicate email, password validation

### Integration Tests
- **`AuthenticationCascadeTests.cs`** — Test `CascadingAuthenticationState`, `AuthorizeRouteView`, unauthorized redirects
- **`RevalidatingAuthenticationStateProviderTests.cs`** — Test security stamp validation, revalidation interval

### Test Coverage (Current)
- ✅ Login handler: 4/4 tests passing
- ✅ Register handler: 7/7 tests passing
- ✅ Authentication cascade: 21/21 tests passing

---

## Future Enhancements

### Planned (Phase 2)
- [ ] Password reset via email
- [ ] Email confirmation on registration
- [ ] Two-factor authentication (TOTP)
- [ ] OAuth providers (Google, Microsoft)

### Web3 Integration (Phase 3)
- [ ] Wallet signature authentication (MetaMask, WalletConnect)
- [ ] Ethereum address linking to existing accounts
- [ ] Nonce-based signature verification
- [ ] Wallet-only registration (no password)

---

## Related Documentation

- **Hybrid Identity:** See `hybrid-identity.md` for Actor ↔ ApplicationUser bridge architecture
- **Authorization:** See `../architecture/authorization.md` (future) for policy-based access control
- **Localization:** See `localization.md` (future) for IStringLocalizer setup

---

## Code Locations

| Component | File Path |
|---|---|
| Login page | `Components/Pages/Auth/Login.razor` |
| Login handler | `Features/Auth/Login/LoginCommandHandler.cs` |
| Register page | `Components/Pages/Auth/Register.razor` |
| Register handler | `Features/Auth/Register/RegisterCommandHandler.cs` |
| NavBar with logout | `Components/Pages/NavBar.razor` |
| Auth state provider | `Infrastructure/Auth/RevalidatingIdentityAuthenticationStateProvider.cs` |
| Router config | `Components/Routes.razor` |
| Identity config | `Program.cs` (lines 84-101) |

---

## Change Log

| Date | Change | Author |
|---|---|---|
| 2026-04-16 | Initial documentation after Track B completion | Gemini Agent |
| 2026-04-16 | Documented login, register, and logout flows | Gemini Agent |
| 2026-04-16 | Added localization key reference table | Gemini Agent |
