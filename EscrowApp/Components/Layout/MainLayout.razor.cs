using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace EscrowApp.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private EscrowApp.Services.ThemeService ThemeService { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Nav.LocationChanged += OnLocationChanged;
        // subscribe to theme service to trigger full layout rerender when theme changes
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        try
        {
            // Read stored theme and re-apply to document element to ensure CSS tokens are correct
            var theme = await JS.InvokeAsync<string>("themeManager.get");
            if (!string.IsNullOrEmpty(theme))
            {
                await JS.InvokeVoidAsync("themeManager.set", theme);
            }
        }
        catch
        {
            // fallback: set attribute directly
            try
            {
                await JS.InvokeVoidAsync("eval", "document.documentElement.setAttribute('data-theme', localStorage.getItem('theme') || 'light')");
            }
            catch { }
        }
    }

    private void OnThemeChanged()
    {
        // Force Blazor to rerender layout + child components
        try { InvokeAsync(StateHasChanged); } catch { }
    }

    public void Dispose()
    {
        try { Nav.LocationChanged -= OnLocationChanged; } catch { }
        try { ThemeService.ThemeChanged -= OnThemeChanged; } catch { }
    }
}
