namespace EscrowApp.Components.Pages;

using Microsoft.Extensions.Localization;

/// <summary>
/// Code-behind for Home.razor — composition root.
/// Localization is handled by IStringLocalizer injected per-component.
/// </summary>
public sealed partial class Home
{
    [Microsoft.AspNetCore.Components.Inject]
    private IStringLocalizer<Home> L { get; set; } = default!;
}
