using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace EscrowApp.Components.Pages.Auth;

/// <summary>
/// Registration page — new user signup for Client or Consultant roles.
/// </summary>
public sealed partial class Register : ComponentBase
{
    [Inject] private IStringLocalizer<Register> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    // TODO: Implement registration flow
    // 1. Collect email, password, display name, role selection
    // 2. Create user via Identity/Entra ID
    // 3. Create Actor entity in domain
    // 4. Create IdentityMapping linking external identity to Actor
    // 5. Redirect to appropriate dashboard based on role
}
