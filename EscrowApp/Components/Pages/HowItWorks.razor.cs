namespace EscrowApp.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

/// <summary>
/// Code-behind for HowItWorks.razor — 3-step workflow display.
/// </summary>
public sealed partial class HowItWorks
{
    [Inject]
    private IStringLocalizer<HowItWorks> L { get; set; } = default!;
}
