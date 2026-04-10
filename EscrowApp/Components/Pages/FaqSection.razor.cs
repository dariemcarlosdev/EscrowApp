namespace EscrowApp.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

/// <summary>
/// Code-behind for FaqSection.razor — collapsible FAQ items.
/// </summary>
public sealed partial class FaqSection
{
    [Inject]
    private IStringLocalizer<FaqSection> L { get; set; } = default!;
}
