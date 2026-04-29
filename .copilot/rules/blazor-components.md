# Blazor Component Rules

## Mandatory Code-Behind Pattern

Every component produces **three files** — no exceptions:

```
ComponentName.razor       ← Markup only. No @code {} blocks. Ever.
ComponentName.razor.cs    ← sealed partial class. All logic here.
ComponentName.razor.css   ← Scoped CSS. Bootstrap 5 + custom overrides.
```

## .razor — Markup Only

- HTML + Razor directives + component references
- `@inject IStringLocalizer<SharedResource> L` is the only allowed inject in markup
- All user-facing strings use `@L["KeyName"]` — no hardcoded text
- Use `@attribute [Authorize]` on every page
- Use `@attribute [StreamRendering]` for pages with async data loading

## .razor.cs — Code-Behind

- Must be `sealed partial class` matching the `.razor` filename
- Inject services via `[Inject]` attribute properties
- Use `[CascadingParameter] Task<AuthenticationState>` for auth — never `IHttpContextAccessor`
- Override `OnInitializedAsync` for data loading — never the constructor
- Use `IMediator.Send()` for all data operations — never call repos/services directly
- Implement `IDisposable` when using `CancellationTokenSource`, timers, or JS interop

## .razor.css — Scoped Styles

- Component-scoped CSS only — no global style overrides
- Use Bootstrap 5 utility classes as the primary styling approach
- Use `::deep` only when absolutely necessary for child component styling
- No inline `style="..."` attributes in markup

## Bootstrap 5 Conventions

| Element | Classes |
|---------|---------|
| Primary actions | `btn btn-primary` |
| Data tables | `table table-striped table-hover` |
| Status badges | `badge bg-success`, `badge bg-warning text-dark`, `badge bg-danger` |
| Layout | `container-fluid`, `row`, `col-md-*` |
| Spacing | Bootstrap spacing utilities (`mt-3`, `mb-4`, `p-3`) |

## Localization

- Supported locales: `en-US` (default), `es-MX`
- Resource files: `Resources/SharedResource.resx` and `SharedResource.es.resx`
- All user-facing strings must be localized — zero hardcoded UI text
