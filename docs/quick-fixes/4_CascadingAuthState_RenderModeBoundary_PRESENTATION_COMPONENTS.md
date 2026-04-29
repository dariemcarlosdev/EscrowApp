# QF-004 — `CascadingAuthenticationState` Does Not Cross Interactive Render Mode Boundary

**Date:** 2026-04-16  
**Layer / Concern:** Presentation — Component Rendering (`Components/Pages/NavBar.razor`)  
**Severity:** 🔴 Circuit crash — `AuthorizeView` inside interactive component has no cascade  

---

## Symptom

```
System.InvalidOperationException: Authorization requires a cascading parameter of
type Task<AuthenticationState>. Consider using CascadingAuthenticationState to
supply this.
   at Microsoft.AspNetCore.Components.Authorization.AuthorizeViewCore
      .OnParametersSetAsync()
```

Occurred on every page load, crashing the circuit immediately after SignalR connected.

---

## Root Cause

`Routes.razor` wraps the entire router in `<CascadingAuthenticationState>`:

```razor
<CascadingAuthenticationState>
    <Router ...>
        <Found Context="routeData">
            <AuthorizeRouteView ... />
        </Found>
    </Router>
</CascadingAuthenticationState>
```

`Home.razor` uses `@rendermode InteractiveServer`, which creates an **interactive
rendering island**. In Blazor's unified rendering model, **cascading values do not
cross render mode boundaries**. The static SSR cascade from `Routes.razor` is
invisible to `Home.razor` and any component it renders, including `NavBar.razor`.

`NavBar.razor` uses `<AuthorizeView>` to show login/logout controls. Because
`NavBar` is rendered inside the interactive island of `Home.razor`, it never
received the `Task<AuthenticationState>` cascade.

---

## Fix

Added a dedicated `<CascadingAuthenticationState>` wrapper directly around the
`<AuthorizeView>` inside `NavBar.razor`. Each interactive island that uses
authorization needs its own cascade source.

**File:** `EscrowApp/Components/Pages/NavBar.razor`

```razor
@* CascadingAuthenticationState is required here because NavBar is used inside
   @rendermode InteractiveServer components where the outer Routes.razor cascade
   does not cross the render-mode boundary. *@
<CascadingAuthenticationState>
    <AuthorizeView>
        <Authorized>...</Authorized>
        <NotAuthorized>...</NotAuthorized>
    </AuthorizeView>
</CascadingAuthenticationState>
```

---

## Rule Going Forward

> **Any component that uses `<AuthorizeView>` or `[Authorize]` and is rendered
> inside an `@rendermode InteractiveServer` / `InteractiveWebAssembly` island
> must either:**
> 1. Wrap its authorization logic in its own `<CascadingAuthenticationState>`, **or**
> 2. Inject `AuthenticationStateProvider` directly and call
>    `GetAuthenticationStateAsync()` manually.

---

## Verification

✅ `NavBar` renders without circuit crash.  
✅ Login/logout controls show correct state on `Home.razor`.  
✅ No duplicate `CascadingAuthenticationState` warning in logs.
