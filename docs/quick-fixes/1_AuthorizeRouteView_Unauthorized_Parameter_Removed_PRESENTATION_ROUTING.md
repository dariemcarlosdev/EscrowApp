# QF-001 — `AuthorizeRouteView.Unauthorized` Parameter Removed in .NET 10

**Date:** 2026-04-16  
**Layer / Concern:** Presentation — Routing (`Components/Routes.razor`)  
**Severity:** 🔴 App crash on every page load  

---

## Symptom

```
System.InvalidOperationException: Object of type
'Microsoft.AspNetCore.Components.Authorization.AuthorizeRouteView'
does not have a property matching the name 'Unauthorized'.
```

The application crashed immediately on startup before any page rendered.

---

## Root Cause

`AuthorizeRouteView` dropped the `Unauthorized` attribute parameter in .NET 10.
The old API accepted a `Type` reference to a redirect page:

```razor
<!-- .NET 8 / 9 — no longer valid in .NET 10 -->
<AuthorizeRouteView Unauthorized="typeof(Pages.Unauthorized)" />
```

In .NET 10 the unauthorized content is expressed as a render-fragment child:

```razor
<AuthorizeRouteView>
    <NotAuthorized>
        <Pages.Unauthorized />
    </NotAuthorized>
</AuthorizeRouteView>
```

---

## Fix

**File:** `EscrowApp/Components/Routes.razor`

```razor
<!-- Before -->
<AuthorizeRouteView RouteData="routeData"
                    DefaultLayout="typeof(Layout.MainLayout)"
                    Unauthorized="typeof(Pages.Unauthorized)" />

<!-- After -->
<AuthorizeRouteView RouteData="routeData"
                    DefaultLayout="typeof(Layout.MainLayout)">
    <NotAuthorized>
        <Pages.Unauthorized />
    </NotAuthorized>
</AuthorizeRouteView>
```

---

## Verification

✅ App starts without `InvalidOperationException`.  
✅ Unauthenticated access to a protected route renders `Unauthorized.razor`.
