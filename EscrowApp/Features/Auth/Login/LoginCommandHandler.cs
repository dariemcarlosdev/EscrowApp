using MediatR;
using Microsoft.AspNetCore.Identity;
using EscrowApp.Models;

namespace EscrowApp.Features.Auth.Login;

/// <summary>
/// Handler for LoginCommand — delegates to SignInManager for credential validation,
/// then resolves the correct post-login destination based on the user's role.
/// </summary>
public sealed class LoginCommandHandler(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await signInManager.PasswordSignInAsync(
            userName: request.Email,
            password: request.Password,
            isPersistent: request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            var roles = user is not null ? await userManager.GetRolesAsync(user) : [];

            var redirectUrl = roles.Contains("Consultant")
                ? "/dashboard/consultant"
                : "/dashboard/client";

            return LoginResult.SuccessResult(redirectUrl);
        }

        if (result.IsLockedOut)
            return LoginResult.FailureResult("Account is locked due to too many failed login attempts. Try again later.");

        if (result.RequiresTwoFactor)
            return LoginResult.FailureResult("Two-factor authentication required.");

        return LoginResult.FailureResult("Invalid email or password.");
    }
}
