# 07 — Localization

> Multi-language support with `IStringLocalizer<T>`, .resx resource files,
> cookie-based culture switching, and per-component translations.

## Status: Implemented (English + Spanish)

---

## Overview

NexTruzt.io supports **English (en)** and **Spanish (es)** using ASP.NET Core's
built-in localization framework. Each Blazor component has its own `.resx` resource
files, plus a `SharedResource` for cross-cutting strings (navigation, buttons,
validation messages, status labels). Culture is persisted via a cookie and switchable
at runtime through the NavBar language toggle.

## Architecture

```
┌─────────────────────────────────┐
│         Blazor Component        │
│  @inject IStringLocalizer<T> L  │
│  @L["KeyName"]                  │
└────────────────┬────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│    Resource File Resolution     │
│  Resources/Components/Pages/    │
│  {Component}.{culture}.resx     │
│  ──or──                         │
│  Resources/SharedResource.resx  │
└────────────────┬────────────────┘
                 │
                 ▼
┌─────────────────────────────────┐
│    Culture Provider (Cookie)    │
│  .AspNetCore.Culture=c=es|uic=es│
└─────────────────────────────────┘
```

## Supported Cultures

| Culture    | Display Name | Default |
| ---------- | ------------ | ------- |
| `en` / `en-US` | English  | ✅      |
| `es` / `es-MX` | Español  | —       |

## Resource File Structure

### Per-Component Resources

Located in `Resources/Components/Pages/`:

| Component               | English (.resx)                   | Spanish (.es.resx)                    |
| ----------------------- | --------------------------------- | ------------------------------------- |
| `Home`                  | `Home.resx`                       | `Home.es.resx`                        |
| `NavBar`                | `NavBar.resx`                     | `NavBar.es.resx`                      |
| `HeroSection`           | `HeroSection.resx`                | `HeroSection.es.resx`                 |
| `HowItWorks`            | `HowItWorks.resx`                 | `HowItWorks.es.resx`                  |
| `SocialProof`           | `SocialProof.resx`                | `SocialProof.es.resx`                 |
| `FaqSection`            | `FaqSection.resx`                 | `FaqSection.es.resx`                  |
| `Footer`                | `Footer.resx`                     | `Footer.es.resx`                      |
| `ClientDashboard`       | `Dashboard/ClientDashboard.resx`       | `Dashboard/ClientDashboard.es.resx`       |
| `ConsultantDashboard`   | `Dashboard/ConsultantDashboard.resx`   | `Dashboard/ConsultantDashboard.es.resx`   |
| `TransactionDetail`     | `Dashboard/TransactionDetail.resx`     | `Dashboard/TransactionDetail.es.resx`     |

### SharedResource (Cross-Cutting)

Located in `Resources/`:

| File                       | Purpose                    |
| -------------------------- | -------------------------- |
| `SharedResource.resx`      | English (default fallback) |
| `SharedResource.es.resx`   | Spanish translations       |

**Marker class:**

```csharp
// File: SharedResource.cs
namespace EscrowApp;
public sealed class SharedResource { }
```

### Key Resource Categories in SharedResource

| Category       | Example Keys                                           | English Values                    |
| -------------- | ------------------------------------------------------ | --------------------------------- |
| **Actions**    | `Submit`, `Cancel`, `Save`, `Delete`, `Login`          | Submit, Cancel, Save, Delete…     |
| **Validation** | `FieldRequired`, `InvalidEmail`, `InvalidAmount`       | Field is required, Invalid email… |
| **Status**     | `StatusPending`, `StatusHeld`, `StatusDisputed`         | Pending, Funds Held, Disputed…    |
| **Navigation** | `NavHome`, `NavDashboard`, `NavTransactions`            | Home, Dashboard, Transactions…    |
| **Errors**     | `ErrorGeneric`, `SessionExpired`, `Unauthorized`        | An error occurred…                |
| **Languages**  | `LanguageEnglish`, `LanguageSpanish`                   | English, Español                  |

## Culture Switching

### Cookie-Based Persistence

Culture is stored in a cookie named `.AspNetCore.Culture`:

```
.AspNetCore.Culture=c%3Des%7Cuic%3Des
```

### Switch Endpoint

```csharp
// Program.cs
app.MapPost("/culture/set", (HttpContext context, string culture, string redirectUri) =>
{
    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
    );
    return Results.LocalRedirect(redirectUri);
});
```

### NavBar Toggle

```csharp
// File: Components/Pages/NavBar.razor.cs
private void SwitchCulture()
{
    var newCulture = CultureInfo.CurrentCulture.Name.StartsWith("es") ? "en" : "es";
    var uri = $"/culture/set?culture={newCulture}&redirectUri={Uri.EscapeDataString(Nav.Uri)}";
    Nav.NavigateTo(uri, forceLoad: true);
}
```

The toggle determines the current culture and switches to the opposite, triggering a
full-page reload to apply the new culture server-side.

## DI Configuration

```csharp
// Program.cs
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Middleware
var supportedCultures = new[] { "en", "es" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));
```

## Usage in Components

```razor
@* HeroSection.razor *@
@inject IStringLocalizer<HeroSection> L

<h1>@L["Headline"]</h1>
<p>@L["Subheadline"]</p>
<button>@L["JoinWaitlist"]</button>
```

## Adding a New Language

1. Create `Resources/SharedResource.{culture}.resx` (e.g., `SharedResource.fr.resx`)
2. Create per-component files in `Resources/Components/Pages/{Component}.{culture}.resx`
3. Add culture to `supportedCultures` array in `Program.cs`
4. No code changes needed — the framework resolves automatically

## Source Files

| File                                                 | Purpose                              |
| ---------------------------------------------------- | ------------------------------------ |
| `SharedResource.cs`                                  | Marker class for shared localizer    |
| `Resources/SharedResource.resx`                      | English shared strings (177 keys)    |
| `Resources/SharedResource.es.resx`                   | Spanish shared translations          |
| `Resources/Components/Pages/*.resx`                  | Per-component English strings (landing page) |
| `Resources/Components/Pages/*.es.resx`               | Per-component Spanish translations (landing page) |
| `Resources/Components/Pages/Dashboard/*.resx`        | Per-component English strings (dashboards) |
| `Resources/Components/Pages/Dashboard/*.es.resx`     | Per-component Spanish translations (dashboards) |
| `Program.cs`                                         | Localization DI + middleware config  |
| `Components/Pages/NavBar.razor.cs`                   | Culture toggle logic                 |
