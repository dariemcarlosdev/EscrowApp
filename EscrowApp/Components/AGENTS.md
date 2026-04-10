# Components — Blazor Server UI

- MANDATORY: Code-behind pattern with 3 files per component:
  - .razor (markup only — NO @code blocks)
  - .razor.cs (sealed partial class — all logic)
  - .razor.css (scoped CSS — Bootstrap 5)
- Use IMediator.Send() for all data operations — never inject repositories
- Use IStringLocalizer<SharedResource> for all user-facing text
- Use [CascadingParameter] Task<AuthenticationState> for auth
- Implement IDisposable when using CancellationTokenSource
- Apply [Authorize] on every page
