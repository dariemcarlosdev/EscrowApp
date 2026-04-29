---
description: Create a new Blazor component with code-behind pattern
---

1. Create component directory under `EscrowApp/Components/Pages/{ComponentName}/`

2. Create three files (MANDATORY — no exceptions):
   - `{ComponentName}.razor` — Markup only, no @code blocks
   - `{ComponentName}.razor.cs` — Sealed partial class with all logic
   - `{ComponentName}.razor.css` — Scoped CSS with Bootstrap 5

3. In `.razor.cs`:
   - Add `[Inject] IMediator Mediator` for data operations
   - Add `[Inject] IStringLocalizer<SharedResource> L` for localization
   - Override `OnInitializedAsync` for data loading
   - Implement `IDisposable` if using CancellationTokenSource

4. Add localization keys to `Resources/SharedResource.resx` and `.es.resx`

5. Run build
   dotnet build EscrowApp.sln // turbo
