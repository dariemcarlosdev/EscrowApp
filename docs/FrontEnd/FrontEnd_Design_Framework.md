# Front-End Design Framework — NexTruzt.io

> The design standard every UI change in `EscrowApp` must follow.
> **Last synced with codebase:** 2026-07-29 · **Status:** Active (Phase 1 — design system + landing shipped)
> **Governing rule:** [`.claude/rules/frontend-aesthetics.md`](../../.claude/rules/frontend-aesthetics.md) — 🔴 STRONG, non-negotiable.

Design like a senior product designer shipping a real brand — **not** like an AI filling a template.
**The test:** if a screen could drop unchanged into any random AI-generated SaaS demo, it fails — revise before shipping.

---

## 1. Why this exists

`EscrowApp` is a **Blazor Server (.NET 10)** fintech product handling money movement. The UI must read as
*trustworthy, deliberate, and institutional* — not decorative. This framework is the single source of truth for
tokens, type, color, motion, and component structure so any developer produces a consistent, on-brand surface
without re-deciding fundamentals.

| Principle | What it means here |
|---|---|
| **Trust over flash** | Restraint signals a financial product. Color carries meaning (status, CTA), never decoration. |
| **Content-first** | Design around real localized copy and real data states (empty / loading / error), not filler. |
| **One focal point per section** | Vary weight, scale, and space on purpose — not everything centered, not everything equal. |
| **Token-driven** | Never hardcode a hex/px in a component. Consume CSS variables so themes and future retunes are free. |
| **Motion with meaning** | Animation supports reading order; subtle, fast, and always reduced-motion-safe. |

---

## 2. Banned "AI tells" (reject on sight)

These are the specific patterns the strong rule forbids. If a diff introduces one, it does not ship.

| ❌ Banned tell | ✅ Do instead |
|---|---|
| Purple/violet → pink/blue "AI gradient" as a brand device | Restrained navy + blue primary, **teal** trust-secondary, **gold** micro-accent |
| Gradient text on headings | Solid ink headings; gradient reserved for the **brand wordmark only** (`.text-gradient`) |
| Glassmorphism / backdrop-blur on every surface | Solid card surfaces (`.glass-panel` is now solid + faint blur accent); frosting is a rare deliberate accent |
| Floating blurred background "blobs" as default decor | At most one restrained ambient element at very low opacity |
| Emoji used as icons (✅, 🔒) | **Bootstrap Icons** (`<i class="bi bi-…">`) or inline SVG — the sanctioned icon set |
| Pill (`border-radius: 50px`) on every button/input | Deliberate radius scale (`--radius-sm/md/lg`); pill only for true chips/tags |
| Neon glows + heavy multi-layer shadows | Single restrained elevation (`--shadow-sm/md`) |
| Symmetric row of 3 identical icon-circle cards, equal weight | Editorial hierarchy/asymmetry; sequence markers where order is real (steps) |
| Inter-only, no typographic point of view | **Space Grotesk** display + **Inter** body — a real voice |
| Fake social proof, invented logos/metrics, lorem copy | Honest, localized copy only. No fabricated numbers or logos. |

---

## 3. Design tokens (source of truth)

All tokens live in [`EscrowApp/wwwroot/app.css`](../../EscrowApp/wwwroot/app.css) as CSS custom properties.
`:root` holds the **dark** values; `[data-theme="light"]` overrides for the **premium light default**.
**Consume the variable — never the literal value.** Retuning values (not renaming vars) updates every scoped
`.razor.css` for free.

### Color

| Role | Var | Light (default) | Dark |
|---|---|---|---|
| Background | `--bg` | `#f4f7fb` | `#020617` |
| Surface / card | `--surface` / `--glass-bg` | `#ffffff` | `#0f172a` / `rgba(30,41,59,.7)` |
| Text primary | `--text-primary` | `#0b1220` | `#f8fafc` |
| Text secondary | `--text-secondary` | `#334155` | `#a8b8cc` |
| Primary accent (blue) | `--accent-blue` | `#2563eb` | `#3b82f6` |
| **Trust secondary (teal)** | `--accent-teal` | `#0f766e` | `#14b8a6` |
| **Premium micro-accent (gold)** | `--accent-gold` | `#A16207` | `#f59e0b` |
| Success | `--accent-green` | `#16a34a` | `#22c55e` |
| Danger | `--accent-red` | `#ef4444` | `#ef4444` |

- Every accent has an `-rgb` companion (e.g. `--accent-teal-rgb`) for `rgba()` alpha usage.
- `--accent-magenta` is **deprecated** — kept only so legacy refs resolve. Never use it in new work.
- Meaning mapping: **blue = primary action**, **teal = trust / security / progress**, **gold = premium emphasis (sparingly)**,
  **green = success/funds-secured**, **red = danger/error**.

### Radius, elevation, motion tokens

| Token | Value | Use |
|---|---|---|
| `--radius-sm` | `8px` | Inputs, small controls |
| `--radius-md` | `12px` | Buttons, default cards' inner elements |
| `--radius-lg` | `16px` | Panels / cards (`.glass-panel`) |
| `--radius-pill` | `999px` | **Chips/tags/badges only** — never default buttons |
| `--shadow-sm` | theme-aware | Resting elevation |
| `--shadow-md` | theme-aware | Hover / raised elevation |

---

## 4. Typography

Loaded via Google Fonts in [`App.razor`](../../EscrowApp/Components/App.razor) (CSP already allows `fonts.googleapis.com`).

| Var | Family | Applied to |
|---|---|---|
| `--font-display` | **Space Grotesk** (400–700) | All headings (`h1–h6`), numerals, stat values, buttons |
| `--font-body` | **Inter** (400–900) | Body, UI, labels |

- Headings get `letter-spacing: -0.015em` globally; hero headline tightens to `-0.02em`.
- Type scale uses Bootstrap's `display-*` / `lead` / `small` utilities — do not invent ad-hoc font sizes.
- Body base 16px, line-height ≥ 1.5. Never body text < 12px.

---

## 5. Layout, surfaces & buttons

| Piece | Rule |
|---|---|
| **Grid** | Bootstrap 5 grid. Mobile-first. No fixed-px container widths. No horizontal scroll at any breakpoint. |
| **Card / panel** | `.glass-panel` = solid `--glass-bg` + 1px `--glass-border` + `--radius-lg` + `--shadow-sm`; hover lifts `translateY(-2px)` to `--shadow-md`. |
| **Primary CTA** | `.btn-glass` — solid `--accent-blue`, white text, `--radius-md`, display font. No gradient/pill/glow. |
| **Secondary / outline** | Bootstrap `btn-outline-*` with a considered radius (`rounded-3`), never `rounded-pill`. |
| **Chips / badges / status** | `--radius-pill` is correct here (e.g. funds-status badge). |
| **Editorial bias** | Left-align content where it aids scanning (e.g. step cards). Reserve full-center for hero and section eyebrows. |

---

## 6. Motion standard (GSAP + ScrollTrigger)

framer-motion is React-only and **cannot** run in Blazor. NexTruzt uses **GSAP 3.12.5 + ScrollTrigger**,
**self-hosted** in [`wwwroot/lib/gsap/`](../../EscrowApp/wwwroot/lib/gsap/) (CSP is `script-src 'self'` — no CDN, no CSP change).
Controller: [`wwwroot/js/motion.js`](../../EscrowApp/wwwroot/js/motion.js), exposing `window.nexMotion.init()`.

### How to animate a section

Declarative — add a data attribute, no JS per component:

| Attribute | Effect | Put it on |
|---|---|---|
| `data-reveal` | Fade + 18px rise as it enters the viewport | A section header / single block |
| `data-reveal-stagger` | Staggers **direct children** (~0.07s each) | A card row / list whose items should cascade |

Init is fired once from [`Home.razor.cs`](../../EscrowApp/Components/Pages/Home.razor.cs) `OnAfterRenderAsync(firstRender)`
and re-scans on `spa:navigation`.

### Non-negotiable motion rules

- **Fail-open:** content is visible by default in HTML/CSS. If GSAP is absent or `prefers-reduced-motion: reduce`,
  `nexMotion.init()` does nothing and nothing hides.
- Durations **0.35–0.5s**, `power2.out`. No infinite decorative loops (a scroll-hint affordance is the only exception).
- Never animate `width`/`height` (layout thrash) — transform + opacity only.
- Above-the-fold content is **not** scroll-revealed; the hero owns a single CSS entrance instead.
- One idempotent init; never double-wrap a section in both a `data-reveal` parent and internal reveal attributes.

---

## 7. Accessibility & responsive baseline

| Check | Requirement |
|---|---|
| Contrast | ≥ 4.5:1 body text on its surface, both themes |
| Focus | `:focus-visible` ring present (global rule in `app.css`); never remove focus outlines |
| Touch target | Interactive controls ≥ 44×44px, ≥ 8px apart |
| Icons | Decorative icons `aria-hidden="true"`; icon-only controls need an `aria-label` |
| Reduced motion | Global `@media (prefers-reduced-motion: reduce)` neutralizes animation; GSAP guarded too |
| Breakpoints | Verify 375 / 768 / 1024 / 1440 — no horizontal scroll, no clipped content |

---

## 8. Localization & regulatory (hard rules)

- **All** user-facing copy goes through `IStringLocalizer` (`L["Key"]`), en + es `.resx`. **Never hardcode visible strings.**
- 🔴 The word **"escrow" must never appear in user-facing UI/copy** without legal approval (NexTruzt is not a licensed
  escrow agent). Use approved terminology ("secure payment holding", "funds secured"). This applies to `.razor` and `.resx`.
- Design around **both** locales — copy length differs; layouts must not break in `es-MX`.

---

## 9. Component convention (mandatory)

Every Blazor component = **three files** ([blazor-components rule](../../.claude/rules/README.md)):

```
ComponentName.razor       ← markup only (no @code blocks, no inline style=)
ComponentName.razor.cs    ← sealed partial class, [Inject], lifecycle
ComponentName.razor.css   ← scoped styles (consume tokens; no literal hex/px colors)
```

Scoped CSS wins on specificity over `app.css` for the same class — keep component overrides token-based so they
stay theme-aware (e.g. a scoped `.waitlist-input` must use `var(--radius-md)`, not `50px`).

---

## 10. Pre-ship verification

1. `dotnet build EscrowApp/EscrowApp.csproj` → **0 errors** (regenerates `EscrowApp.styles.css`).
2. Open `/`: reveals fire on scroll, no layout shift; light (default) + dark both correct; no flash on reload.
3. DevTools console → **no CSP violations**, no JS errors on enhanced navigation.
4. `prefers-reduced-motion: reduce` → animations off, content fully visible.
5. Switch culture to `es` → copy localized, layout intact.
6. Scan the diff against §2 — **no AI tell reintroduced**. Scan `.razor`/`.resx` for the word "escrow".
7. Responsive at 375 / 768 / 1024 / 1440.

---

## See also

- [UI Redesign Plan](UI-Redesign-Plan.md) — phased execution, decisions, and status
- [`frontend-aesthetics.md`](../../.claude/rules/frontend-aesthetics.md) — the always-loaded strong rule
- [`app.css`](../../EscrowApp/wwwroot/app.css) — token definitions · [`motion.js`](../../EscrowApp/wwwroot/js/motion.js) — reveal controller
