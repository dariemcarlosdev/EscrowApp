using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace EscrowApp.Components.Shared;

public static class ThemeInterop
{
    // Callback assigned by ThemeService to notify .NET consumers
    public static Action? OnThemeChangedCallback;

    [JSInvokable("ThemeChanged")]
    public static Task ThemeChanged()
    {
        try
        {
            OnThemeChangedCallback?.Invoke();
        }
        catch { }
        return Task.CompletedTask;
    }
}
