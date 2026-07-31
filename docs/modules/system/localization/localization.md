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

## User Stories

Stories for the i18n/l10n stack — IStringLocalizer<T>, per-component .resx files, and cookie-based culture switching.

### Story 1 — Visitor switches language at runtime

**As a** Client or Consultant, **I want** to switch the UI between English and Spanish from the navigation bar, **so that** I can read the platform in my preferred language without re-registering.

**Acceptance Criteria:**

- [ ] the request to /culture/set?culture=es&redirectUri=/ is issued
- [ ] the .AspNetCore.Culture cookie is set with c=es|uic=es
- [ ] the page reloads with all localized strings rendered in Spanish

```gherkin
Feature: Runtime culture toggle
  Scenario: Switch from English to Spanish
    Given I am on the landing page in English
    When I click the "Español" toggle in the NavBar
    Then the request to /culture/set?culture=es&redirectUri=/ is issued
    And the .AspNetCore.Culture cookie is set with c=es|uic=es
    And the page reloads with all localized strings rendered in Spanish
```

### Story 2 — Persistent culture across sessions

**As a** Client, **I want** my language choice to persist across browser sessions, **so that** I do not have to re-select my language every visit.

**Acceptance Criteria:**

- [ ] the page renders in Spanish on first load
- [ ] no language-toggle interaction is required

```gherkin
Feature: Culture cookie persistence
  Scenario: Returning visitor
    Given I previously set culture = "es" and the cookie has not expired
    When I return to the site days later
    Then the page renders in Spanish on first load
    And no language-toggle interaction is required
```

### Story 3 — Per-component resource resolution

**As a** Developer, **I want** each Blazor component to resolve its strings from its own `.resx` (with a shared `SharedResource` for cross-cutting strings), **so that** localization changes do not cascade across unrelated components and key collisions are avoided.

**Acceptance Criteria:**

- [ ] the string is resolved from Resources/Components/Pages/HeroSection.resx
- [ ] not from any other component's resource file

```gherkin
Feature: Per-component resources
  Scenario: HeroSection key resolves from HeroSection.resx
    Given culture is "en"
    When HeroSection.razor renders @L["Headline"]
    Then the string is resolved from Resources/Components/Pages/HeroSection.resx
    And not from any other component's resource file
```


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
