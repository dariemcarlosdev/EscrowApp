using System;

namespace EscrowApp.Services;

public sealed class ThemeService
{
    // Event subscribers will be called when theme changes via JS interop
    public event Action? ThemeChanged;

    public ThemeService()
    {
        // register static interop callback
        EscrowApp.Components.Shared.ThemeInterop.OnThemeChangedCallback = () => ThemeChanged?.Invoke();
    }

    public void NotifyThemeChanged() => ThemeChanged?.Invoke();
}
