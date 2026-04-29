using EscrowApp.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EscrowApp.Infrastructure.Auth;

/// <summary>
/// Revalidating authentication state provider for Blazor Server with ASP.NET Core Identity.
///
/// Extends <see cref="RevalidatingServerAuthenticationStateProvider"/> — the correct Blazor Server
/// base class — which manages a background timer and creates properly scoped DI contexts for
/// each revalidation cycle. This avoids the DI scope violation thrown by
/// <see cref="Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider"/> when
/// called outside a component rendering pipeline.
///
/// On each revalidation tick the user's security stamp is verified against Identity's store.
/// If the stamp has changed (password reset, role change, explicit logout) the circuit is
/// notified and the user is signed out.
/// </summary>
public sealed class RevalidatingIdentityAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(30);

    protected override async Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await ValidateSecurityStampAsync(userManager, authenticationState.User);
    }

    private static async Task<bool> ValidateSecurityStampAsync(
        UserManager<ApplicationUser> userManager,
        ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return false;

        if (!userManager.SupportsUserSecurityStamp)
            return true;

        var principalStamp = principal.FindFirstValue(new ClaimsIdentityOptions().SecurityStampClaimType);
        var userStamp = await userManager.GetSecurityStampAsync(user);
        return principalStamp == userStamp;
    }
}
