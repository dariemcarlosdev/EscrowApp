# UI Redesign Plan — NexTruzt.io

> Execution plan for the front-end redesign. Pairs with the [Front-End Design Framework](FrontEnd_Design_Framework.md) (the standard).
> **Last synced with codebase:** 2026-07-29 · **Status:** Phase 1 complete (design system + landing); Phase 2 pending.

---

## 1. Context

`EscrowApp` (Blazor Server, .NET 10) shipped with a working but **AI-templated** look: a blue→magenta gradient
everywhere, gradient text on every heading, glassmorphism as the default surface, animated background blobs, and
pill-radius on all controls. The goal of this redesign is a **premium, trust-oriented, light-first fintech** surface
that reads as deliberate — governed by the strong rule [`frontend-aesthetics.md`](../../.claude/rules/frontend-aesthetics.md).

This is a **retune, not a rewrite**: the app already had a mature CSS-custom-property token system. We changed token
*values* and retired tells while preserving variable *names*, so the ~15 scoped stylesheets inherited the new look
with near-zero churn.

### Locked decisions

| Decision | Choice | Rationale |
|---|---|---|
| Motion library | **GSAP + ScrollTrigger**, self-hosted | framer-motion is React-only; CSP blocks CDNs → vendored in `wwwroot/lib/gsap/` |
| Default theme | **Light** (premium) | User rejected dark-default; dark remains via toggle |
| Direction | **Modern Institutional** | Geometric/crisp; navy+blue primary, teal trust-secondary, gold micro-accent |
| Type voice | **Space Grotesk** display + **Inter** body | A real typographic POV vs. Inter-only |
| Foundation | Keep Bootstrap 5 + tokens | No new NuGet, no framework swap |
| Scope | Phased | Design system + landing first; inner pages deferred |

---

## 2. Goals / Non-goals

**Goals**
- [x] Every landing screen passes the no-AI-vibe test (§2 of the framework).
- [x] Retune tokens to premium light-trust (blue + teal + gold); retire the `#d946ef` magenta gradient.
- [x] Add GSAP scroll-reveal / stagger to the landing page with reduced-motion + SPA-nav re-init.
- [x] Redesign the landing composition + shared shell against the new tokens.
- [x] Publish this framework + plan under `docs/FrontEnd/`.

**Non-goals (this pass)**
- Inner-page deep restyle: `ClientDashboard`, `ConsultantDashboard`, `TransactionDetail`, `Login`, `Register`,
  `DashboardLayout` → **Phase 2** (they already improve for free via the retuned tokens).
- No new NuGet, no Bootstrap replacement, no route/feature/backend changes.
- No paid GSAP Club plugins (SplitText etc.) — core + ScrollTrigger only.
- No CSP/security-header changes. No `.resx` copy rewrites (only add a key if a genuinely new visible string is needed).

---

## 3. Palette & type decisions

**Palette rationale.** A fintech handling money reads as trustworthy through restraint. Blue anchors primary actions;
**teal** became the trust/security/progress signal (replacing the magenta half of every gradient); **gold** is a rare
premium emphasis. Full token table lives in the [framework §3](FrontEnd_Design_Framework.md#3-design-tokens-source-of-truth).

**Typography rationale.** Pairing a geometric display face (Space Grotesk) for headings/numerals with a neutral,
legible body face (Inter) gives an institutional voice while keeping long-form copy comfortable and multi-locale-safe.

---

## 4. Phased component checklist

### Phase 1 — Foundation + landing ✅ (2026-07-29)

| Area | File(s) | Status |
|---|---|---|
| Tokens (palette, radius, shadow, teal/gold, fonts) | `wwwroot/app.css` | ✅ |
| Fonts + GSAP/motion script wiring; light default | `Components/App.razor` | ✅ |
| Light-default fallback | `wwwroot/js/theme.js` | ✅ |
| GSAP vendored (3.12.5) | `wwwroot/lib/gsap/gsap.min.js`, `ScrollTrigger.min.js` | ✅ |
| Reveal controller | `wwwroot/js/motion.js` | ✅ |
| Motion init | `Components/Pages/Home.razor.cs` | ✅ |
| Landing composition (reveal granularity) | `Components/Pages/Home.razor` | ✅ |
| Hero (two-tone headline, SVG icons, no-pill CTA, softened card) | `HeroSection.razor(.css)` | ✅ |
| How-it-works (teal numerals, editorial cards, de-purpled connector, stagger) | `HowItWorks.razor(.css)` | ✅ |
| Social proof (teal stat values, stagger) | `SocialProof.razor(.css)` | ✅ |
| FAQ (reveal + stagger) | `FaqSection.razor` | ✅ |
| Footer (de-purpled brand grad) | `Footer.razor` | ✅ |
| NavBar (de-purpled brand grad + hover, no-pill login) | `NavBar.razor(.css)` | ✅ |

### Phase 2 — Inner pages (pending)

| Area | File(s) | Notes |
|---|---|---|
| Dashboard shell (retire remaining magenta grad + `.text-gradient` override) | `Layout/DashboardLayout.razor(.css)` | Brand grad + local `.text-gradient` still use `--accent-magenta` |
| Client / Consultant dashboards | `Pages/…Dashboard…` | Apply editorial hierarchy to KPI/panel/table surfaces |
| Transaction detail | `Pages/TransactionDetail…` | Status/timeline surfaces |
| Auth (login / register) | `Pages/…` | Form UX pass against framework §5/§7 |

---

## 5. Motion plan (implemented)

- Vendored GSAP + ScrollTrigger; `motion.js` exposes `nexMotion.init()` — idempotent, reduced-motion + no-lib fail-open,
  re-scans on `spa:navigation`.
- Declarative usage: `data-reveal` (single block) and `data-reveal-stagger` (cascade direct children).
- Reveal attributes sit on **meaningful elements inside** each section (header vs. card group), not blanket-wrapping —
  hero is above the fold and keeps its own CSS entrance.

---

## 6. Acceptance criteria

Build clean (0 errors) · `/` reveals fire without layout shift · light (default) + dark both correct · no CSP console
violations · reduced-motion disables animation · `es` culture localized · diff carries **no** reintroduced AI tell ·
no "escrow" in user-facing copy · responsive 375 / 768 / 1024 / 1440. Full list: [framework §10](FrontEnd_Design_Framework.md#10-pre-ship-verification).

---

## 7. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Token *rename* would break the ~15 scoped stylesheets | Only values changed; names preserved. `--accent-magenta` kept as deprecated alias. |
| Blazor Server + GSAP interop timing | Init in `OnAfterRenderAsync(firstRender)`, JSDisconnected/Cancelled-safe, re-run on SPA nav. |
| Scoped CSS silently overriding tokens (e.g. pill leftover) | Audit scoped `.razor.css` for literal `50px`/hex; force token usage. |
| Regulatory copy drift | Presentation-only; never add "escrow" to visible/localized strings. |
