using EscrowApp.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EscrowApp.Infrastructure.Auth;

/// <summary>
/// Revalidating authentication state provider for Blazor Server with ASP.NET Core Identity.
///
/// Inherits from <see cref="AuthenticationStateProvider"/> and manages security stamp validation.
/// On each revalidation tick the user's security stamp is verified against Identity's store.
/// If the stamp has changed (password reset, role change, explicit logout) the circuit is
/// notified and the user is signed out.
/// </summary>
public sealed class RevalidatingIdentityAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    public RevalidatingIdentityAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
    {
        _loggerFactory = loggerFactory;
        _scopeFactory = scopeFactory;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var httpContext = scope.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var isValid = await ValidateSecurityStampAsync(userManager, httpContext.User);
            if (isValid)
                return new AuthenticationState(httpContext.User);
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Invalidates the current authentication state, triggering a re-evaluation of the user's auth status.
    /// Called on logout or when credentials are revoked.
    /// </summary>
    public void InvalidateAuthState()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
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
