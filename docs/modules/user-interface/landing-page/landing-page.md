# 08 — Landing Page UI

> Blazor Server component architecture with code-behind pattern, scoped CSS,
> glassmorphism design, and full English/Spanish localization.

## Status: Implemented

---

## Overview

The NexTruzt.io landing page is a composition of **7 Blazor components**, each following
the code-behind pattern (`.razor` + `.razor.cs` + `.razor.css`). The design uses a
**glassmorphism** aesthetic with gradient backgrounds, frosted-glass cards, and smooth
animations — built on Bootstrap 5 with custom scoped CSS and CSS custom properties.

## User Stories

Stories for the public landing page composed of 7 Blazor components. **All copy must avoid the word *escrow* in favor of *secure payment holding*, *held funds*, *platform*, or *service fee*.**

### Story 1 — First-time visitor understands the value proposition

**As a** Client visiting for the first time, **I want** the hero section to explain in one sentence how secure payment holding works, **so that** I can decide within 10 seconds whether to keep reading.

**Acceptance Criteria:**

- [ ] the HeroSection headline references "secure payment holding" or equivalent approved copy
- [ ] no user-facing string contains the word "escrow"
- [ ] a clear primary CTA (waitlist or sign-up) is visible without scrolling

```gherkin
Feature: Hero clarity
  Scenario: Above-the-fold messaging
    Given I land on "/"
    When the page renders
    Then the HeroSection headline references "secure payment holding" or equivalent approved copy
    And no user-facing string contains the word "escrow"
    And a clear primary CTA (waitlist or sign-up) is visible without scrolling
```

### Story 2 — Three-step "How it works" is scannable

**As a** Consultant, **I want** the "How it works" section to show the Create → Hold → Release flow in three short steps, **so that** I can explain the platform to clients in my own words.

**Acceptance Criteria:**

- [ ] exactly three steps are visible
- [ ] each step uses approved terminology (no "escrow" in user-facing strings)
- [ ] the steps are localized (en and es)

```gherkin
Feature: How it works clarity
  Scenario: Three steps are present
    Given the landing page is rendered
    When I scroll to the HowItWorks section
    Then exactly three steps are visible
    And each step uses approved terminology (no "escrow" in user-facing strings)
    And the steps are localized (en and es)
```

### Story 3 — Bilingual visitor reads in their language

**As a** Spanish-speaking Client, **I want** every visible string on the landing page to switch to Spanish when I toggle the language, **so that** I can read the entire page without language gaps.

**Acceptance Criteria:**

- [ ] NavBar, HeroSection, HowItWorks, SocialProof, FaqSection, and Footer all render in Spanish
- [ ] no English fallback strings appear in the rendered HTML

```gherkin
Feature: Full landing-page localization
  Scenario: Spanish toggle
    Given I click "Español" in the NavBar
    When the page reloads
    Then NavBar, HeroSection, HowItWorks, SocialProof, FaqSection, and Footer all render in Spanish
    And no English fallback strings appear in the rendered HTML
```

### Story 4 — Visitor joins the waitlist

**As a** Client interested in the platform, **I want** to submit my email to a waitlist from the hero section, **so that** the team can contact me when onboarding opens.

**Acceptance Criteria:**

- [ ] my email is recorded
- [ ] I see a localized confirmation message
- [ ] the email is validated client-side and server-side

```gherkin
Feature: Waitlist capture
  Scenario: Successful waitlist submission
    Given I enter a valid email in the HeroSection waitlist form
    When I submit the form
    Then my email is recorded
    And I see a localized confirmation message
    And the email is validated client-side and server-side
```


## Component Tree

```
App.razor
└── Routes.razor
    └── MainLayout.razor
        ├── Home.razor (/)
        │   ├── NavBar.razor
        │   │   └── Language toggle, brand, navigation links
        │   │
        │   ├── HeroSection.razor
        │   │   ├── Headline + subheadline
        │   │   ├── Waitlist email form
        │   │   └── Escrow simulation card (Hold → Release demo)
        │   │
        │   ├── HowItWorks.razor
        │   │   └── 3-step workflow (Create → Hold → Release)
        │   │
        │   ├── SocialProof.razor
        │   │   └── Trust badges, statistics, testimonials
        │   │
        │   ├── FaqSection.razor
        │   │   └── Collapsible FAQ accordion
        │   │
        │   └── Footer.razor
        │       └── Legal links, copyright, brand
        │
        ├── Dashboard/
        │   ├── ClientDashboard.razor (/dashboard/client) [Authorize]
        │   │   └── Client transaction table with status, actions (stub — data loading not implemented)
        │   ├── ConsultantDashboard.razor (/dashboard/consultant) [Authorize]
        │   │   └── Consultant earnings and transaction list (stub — data loading not implemented)
        │   └── TransactionDetail.razor (/transaction/{id}) [Authorize]
        │       └── Full transaction details, status timeline, actions (stub — data loading not implemented)
        │
        ├── Auth/
        │   ├── Login.razor (/auth/login)
        │   │   └── Login form (UI only — no auth backend)
        │   └── Register.razor (/auth/register)
        │       └── Registration form (UI only — no auth backend)
        │
        ├── Error.razor (/Error)
        │   └── Exception handling page
        └── NotFound.razor (/not-found)
            └── Custom 404 page
```

## Component Details

### Home (`Components/Pages/Home.razor`)

- **Route**: `/`
- **Purpose**: Composition root — assembles all landing page sections
- **Injection**: `IStringLocalizer<Home> L`
- **Pattern**: Thin orchestrator with no business logic

### NavBar (`Components/Pages/NavBar.razor`)

- **Purpose**: Brand identity, navigation links, language toggle
- **Injection**: `IStringLocalizer<NavBar> L`, `NavigationManager Nav`
- **Key Method**: `SwitchCulture()` — toggles between English and Spanish
- **Features**: Responsive hamburger menu, language flag toggle

### HeroSection (`Components/Pages/HeroSection.razor`)

- **Purpose**: Primary call-to-action — headline, waitlist form, escrow demo
- **Injection**: `IStringLocalizer<HeroSection> L`
- **Properties**:
  - `WaitlistEmail` — bound to email input
  - `IsSubmitting` / `SubmitSuccess` / `SubmitError` — form state flags
  - `FundsReleased` — escrow simulation state
- **Key Methods**:
  - `HandleWaitlistSubmit()` — validates and submits waitlist email (simulated)
  - `SimulateRelease()` — animates fund release on the demo card
- **Design**: Glassmorphism card with gradient overlay, animated state transitions

### HowItWorks (`Components/Pages/HowItWorks.razor`)

- **Purpose**: 3-step visual workflow
- **Injection**: `IStringLocalizer<HowItWorks> L`
- **Steps**: Create Escrow → Hold Funds → Release Payment
- **Design**: Icon cards with step numbers, responsive grid

### SocialProof (`Components/Pages/SocialProof.razor`)

- **Purpose**: Build trust — statistics, security badges, user testimonials
- **Injection**: `IStringLocalizer<SocialProof> L`
- **Design**: Counter animations, trust badge icons, testimonial cards

### FaqSection (`Components/Pages/FaqSection.razor`)

- **Purpose**: Answer common questions via collapsible accordion
- **Injection**: `IStringLocalizer<FaqSection> L`
- **Design**: Bootstrap accordion with localized Q&A pairs

### Footer (`Components/Pages/Footer.razor`)

- **Purpose**: Legal links, copyright, brand reinforcement
- **Injection**: `IStringLocalizer<Footer> L`
- **Design**: Dark footer with column layout, social links

## Code-Behind Pattern

Every component follows the strict separation:

```
Components/Pages/
├── HeroSection.razor       ← Markup (Razor template)
├── HeroSection.razor.cs    ← Logic (partial class)
└── HeroSection.razor.css   ← Scoped styles (CSS isolation)
```

```csharp
// HeroSection.razor.cs
namespace EscrowApp.Components.Pages;

public sealed partial class HeroSection
{
    [Inject] private IStringLocalizer<HeroSection> L { get; set; } = default!;

    private string WaitlistEmail { get; set; } = "";
    private bool IsSubmitting { get; set; }
    // ...
}
```

**Rules enforced:**
- No `@code {}` blocks in `.razor` files
- Component class is always `sealed partial`
- Every component has its own `.razor.css` for style isolation
- `IStringLocalizer<T>` injected for all user-facing text

## Design System

### CSS Custom Properties

```css
:root {
    --primary-gradient: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    --glass-bg: rgba(255, 255, 255, 0.08);
    --glass-border: rgba(255, 255, 255, 0.15);
    --glass-blur: blur(20px);
    --text-primary: #ffffff;
    --text-secondary: rgba(255, 255, 255, 0.7);
}
```

### Glassmorphism

Cards and containers use frosted-glass effects:

```css
.glass-card {
    background: var(--glass-bg);
    backdrop-filter: var(--glass-blur);
    border: 1px solid var(--glass-border);
    border-radius: 16px;
}
```

### Responsive Breakpoints

- **Desktop**: Multi-column grid, full animations
- **Tablet**: Reduced columns, stacked layout
- **Mobile**: Single column, hamburger nav, touch-optimized

## Accessibility

| Feature                | Implementation                           |
| ---------------------- | ---------------------------------------- |
| **Contrast**           | WCAG AA compliant text-on-gradient       |
| **ARIA Labels**        | All interactive elements labeled          |
| **Skip to Content**    | Skip-to-content link for keyboard users  |
| **Semantic HTML**      | `<nav>`, `<main>`, `<section>`, `<footer>` |
| **Form Labels**        | Associated `<label>` on all inputs       |
| **Focus Indicators**   | Visible focus rings on interactive items  |

## Layout & Routing

```csharp
// Components/Layout/MainLayout.razor
// Wraps all pages with consistent chrome

// Components/Pages/Home.razor
@page "/"
// Composes all landing page sections
```

Additional routes:
- `/dashboard/client` → `Dashboard/ClientDashboard.razor` (client transaction view)
- `/dashboard/consultant` → `Dashboard/ConsultantDashboard.razor` (consultant earnings dashboard)
- `/transaction/{id}` → `Dashboard/TransactionDetail.razor` (transaction detail view)
- `/auth/login` → `Auth/Login.razor` (login form — UI only, no auth backend)
- `/auth/register` → `Auth/Register.razor` (registration form — UI only, no auth backend)
- `/not-found` → `NotFound.razor` (custom 404 page)
- `/Error` → `Error.razor` (exception handling page)

## Source Files

| File                                   | Purpose                                   |
| -------------------------------------- | ----------------------------------------- |
| `Components/Pages/Home.razor(.cs)`     | Page composition root                     |
| `Components/Pages/NavBar.razor(.cs/.css)` | Navigation + language toggle           |
| `Components/Pages/HeroSection.razor(.cs/.css)` | Hero CTA + escrow demo           |
| `Components/Pages/HowItWorks.razor(.cs/.css)` | 3-step workflow                   |
| `Components/Pages/SocialProof.razor(.cs/.css)` | Trust signals                    |
| `Components/Pages/FaqSection.razor(.cs/.css)` | FAQ accordion                     |
| `Components/Pages/Footer.razor(.cs/.css)` | Page footer                           |
| `Components/Pages/Dashboard/ClientDashboard.razor(.cs/.css)` | Client transaction dashboard |
| `Components/Pages/Dashboard/ConsultantDashboard.razor(.cs/.css)` | Consultant earnings dashboard |
| `Components/Pages/Dashboard/TransactionDetail.razor(.cs/.css)` | Transaction detail view |
| `Components/Pages/Auth/Login.razor(.cs/.css)` | Login form (UI only)             |
| `Components/Pages/Auth/Register.razor(.cs/.css)` | Registration form (UI only)  |
| `Components/Pages/Error.razor`         | Exception handling page                   |
| `Components/Pages/NotFound.razor`      | Custom 404 page                           |
| `Components/Layout/MainLayout.razor`   | App shell layout                          |
| `wwwroot/`                             | Static assets (CSS, images, JS)           |
