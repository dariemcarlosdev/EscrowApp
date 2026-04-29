namespace EscrowApp.Components.Pages;

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

/// <summary>
/// Code-behind for NavBar.razor — brand, language toggle, authentication controls, navigation links.
/// Switches culture via cookie + full page reload. Logout redirects to /auth/logout HTTP endpoint
/// which calls SignOutAsync() over a real HTTP response (cannot be done inside a Blazor circuit).
/// </summary>
public sealed partial class NavBar
{
    [Inject]
    private IStringLocalizer<NavBar> L { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    private bool IsLoggingOut { get; set; }

    private void SwitchCulture()
    {
        var newCulture = CultureInfo.CurrentUICulture.Name == "es" ? "en" : "es";
        var uri = new Uri(Nav.Uri).GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
        var redirectUri = string.IsNullOrEmpty(uri) ? "/" : uri;

        Nav.NavigateTo($"/culture/set?culture={newCulture}&redirectUri={Uri.EscapeDataString(redirectUri)}", forceLoad: true);
    }
}
