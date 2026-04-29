# QF-005 — `EditForm` Missing `FormName` in Blazor SSR Mode

**Date:** 2026-04-16  
**Layer / Concern:** Presentation — Auth Forms (`Components/Pages/Auth/`)  
**Severity:** 🔴 Form submission rejected by framework before handler runs  

---

## Symptom

```
The POST request does not specify which form is being submitted. To fix this,
ensure <form> elements have a @formname attribute with any unique value, or pass
a FormName parameter if using <EditForm>.
```

Both the Register and Login pages were completely non-functional — submitting
either form returned this error immediately.

---

## Root Cause

In Blazor's static SSR rendering model, form `POST` requests are standard HTTP
POST requests handled server-side. When multiple forms can exist in an application,
Blazor needs a `FormName` to route the incoming POST to the correct `EditForm`
handler. Without it, the framework rejects the request before the component's
`OnValidSubmit` callback is ever invoked.

```razor
<!-- Before — no FormName, framework rejects POST -->
<EditForm Model="RegisterModel" OnValidSubmit="HandleRegister">
```

Additionally, `Login.razor` had a **case-sensitivity bug** on the password field:
`@bind-value` (lowercase `v`) is silently ignored by Blazor — `@bind-Value`
(uppercase `V`) is the correct directive. The password was never two-way bound.

---

## Fix

**File:** `EscrowApp/Components/Pages/Auth/Register.razor`

```razor
<!-- After -->
<EditForm Model="RegisterModel" OnValidSubmit="HandleRegister" FormName="RegisterForm">
```

**File:** `EscrowApp/Components/Pages/Auth/Login.razor`

```razor
<!-- After — FormName added + @bind-value typo fixed to @bind-Value -->
<EditForm Model="LoginModel" OnValidSubmit="HandleLogin" FormName="LoginForm">
    ...
    <InputText @bind-Value="LoginModel.Password" />   <!-- was @bind-value -->
```

---

## Verification

✅ Submitting the register form reaches `HandleRegister`.  
✅ Submitting the login form reaches `HandleLogin` with the correct password value.  
✅ No `@formname` framework error in browser or server logs.
