using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EscrowApp.Components.Shared;

public partial class ThemeToggle : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private string _currentTheme = "light";
    protected bool IsLight => _currentTheme == "light";
    protected string IconClass => IsLight ? "bi-sun" : "bi-moon-stars";

    private bool _initialized = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_initialized)
        {
            _initialized = true;
            try
            {
                var theme = await JS.InvokeAsync<string>("themeManager.get");
                if (!string.IsNullOrEmpty(theme))
                {
                    _currentTheme = theme;
                }
            }
            catch
            {
                // fallback: read localStorage directly via eval
                try
                {
                    var stored = await JS.InvokeAsync<string>("eval", "localStorage.getItem('theme')");
                    if (!string.IsNullOrEmpty(stored)) _currentTheme = stored;
                }
                catch { /* ignore */ }
            }
            // ensure document has attribute set
            try
            {
                await JS.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{_currentTheme}');");
            }
            catch { }
            StateHasChanged();
        }
    }

    protected async Task ToggleTheme()
    {
        _currentTheme = IsLight ? "dark" : "light";
        try
        {
            await JS.InvokeVoidAsync("console.log", "[ThemeToggle] toggling ->", _currentTheme);
            await JS.InvokeVoidAsync("themeManager.set", _currentTheme);
            await JS.InvokeVoidAsync("console.log", "[ThemeToggle] themeManager.set called");
        }
        catch
        {
            // fallback: set attribute and localStorage via eval
            try
            {
                await JS.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{_currentTheme}'); localStorage.setItem('theme', '{_currentTheme}');");
                await JS.InvokeVoidAsync("console.log", "[ThemeToggle] fallback set applied ->", _currentTheme);
            }
            catch { }
        }
        StateHasChanged();
    }
}
