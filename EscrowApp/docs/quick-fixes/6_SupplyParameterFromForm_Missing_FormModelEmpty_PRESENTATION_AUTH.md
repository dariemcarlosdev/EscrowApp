# QF-006 — `[SupplyParameterFromForm]` Missing — Form Model Always Empty in SSR

**Date:** 2026-04-16  
**Layer / Concern:** Presentation — Auth Code-Behind (`Components/Pages/Auth/`)  
**Severity:** 🔴 Form data silently discarded — Identity validates empty strings  

---

## Symptom

Registering with `Admin123!` produced every Identity password rule error simultaneously:

```
Passwords must be at least 8 characters.
Passwords must have at least one digit ('0'-'9').
Passwords must have at least one lowercase ('a'-'z').
Passwords must have at least one uppercase ('A'-'Z').
Passwords must use at least 1 different characters.
```

`Admin123!` satisfies all rules — the errors confirmed the password field was empty.

---

## Root Cause

In Blazor's static SSR model, a form `POST` is a raw HTTP request. The framework
does **not** automatically populate a component property from form body values
unless the property is decorated with `[SupplyParameterFromForm]`.

Without the attribute, the model stays at its initialized value of `new()` —
all string fields are `string.Empty`. Identity then validates an empty password
against all its rules and fails every check.

```csharp
// Before — model is never populated from the POST body
private RegisterFormModel RegisterModel { get; set; } = new();
```

The `FormName` on `[SupplyParameterFromForm]` must match the `FormName` on the
corresponding `<EditForm>` so Blazor knows which form's fields to bind.

---

## Fix

**File:** `EscrowApp/Components/Pages/Auth/Register.razor.cs`

```csharp
// After — Blazor populates this from the POST body before HandleRegister runs
[SupplyParameterFromForm(FormName = "RegisterForm")]
private RegisterFormModel RegisterModel { get; set; } = new();
```

**File:** `EscrowApp/Components/Pages/Auth/Login.razor.cs`

```csharp
[SupplyParameterFromForm(FormName = "LoginForm")]
private LoginFormModel LoginModel { get; set; } = new();
```

---

## Verification

✅ `Admin123!` passes all Identity password validations.  
✅ User successfully created in `AspNetUsers` table.  
✅ Login model receives correct email, password, and `RememberMe` from form POST.
