using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace EscrowApp.Components.Layout;

public sealed partial class DashboardLayout : LayoutComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = default!;

    private string DashboardHref => Navigation.Uri.Contains("consultant")
        ? "/dashboard/consultant"
        : "/dashboard/client";

    private string ActiveClass(string segment) =>
        Navigation.Uri.Contains(segment) ? "active" : string.Empty;
}
