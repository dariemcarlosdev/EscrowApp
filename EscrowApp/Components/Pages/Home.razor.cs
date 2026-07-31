namespace EscrowApp.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

/// <summary>
/// Code-behind for Home.razor — composition root.
/// Localization is handled by IStringLocalizer injected per-component.
/// </summary>
public sealed partial class Home
{
    [Inject]
    private IStringLocalizer<Home> L { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Kick off the GSAP scroll-reveal choreography once the interactive circuit
    /// has rendered the landing DOM. Fails open: any interop error leaves content visible.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            await JS.InvokeVoidAsync("nexMotion.init");
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected during teardown — nothing to animate.
        }
        catch (OperationCanceledException)
        {
            // Navigation cancelled the render — safe to ignore.
        }
    }
}
