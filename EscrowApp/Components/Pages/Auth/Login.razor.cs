using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace EscrowApp.Components.Pages.Auth;

/// <summary>
/// Login page — entry point for user authentication.
///
/// IMPLEMENTATION REQUIRED: Choose auth strategy and implement:
/// - ASP.NET Core Identity (simplest for MVP)
/// - Microsoft Entra ID (production-grade)
/// - Duende IdentityServer (self-hosted OIDC)
///
/// See docs/cross-cutting/hybrid-identity/hybrid-identity.md for architecture guidance.
/// </summary>
public sealed partial class Login : ComponentBase
{
    [Inject] private IStringLocalizer<Login> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // TODO: Implement login logic based on chosen auth provider
    // For Entra ID: Navigation.NavigateTo("/MicrosoftIdentity/Account/SignIn", true);
    // For Identity: form submission → SignInManager.PasswordSignInAsync()
}
