namespace EscrowApp.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

/// <summary>
/// Code-behind for SocialProof.razor — trust badges and stats.
/// </summary>
public sealed partial class SocialProof
{
    [Inject]
    private IStringLocalizer<SocialProof> L { get; set; } = default!;
}
