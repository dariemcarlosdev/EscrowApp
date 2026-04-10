namespace EscrowApp.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

/// <summary>
/// Code-behind for Footer.razor — legal links, copyright, brand.
/// </summary>
public sealed partial class Footer
{
    [Inject]
    private IStringLocalizer<Footer> L { get; set; } = default!;
}
