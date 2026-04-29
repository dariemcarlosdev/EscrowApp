using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MediatR;
using EscrowApp.Features.Auth.Login;

namespace EscrowApp.Components.Pages.Auth;

/// <summary>
/// Login page — entry point for user authentication via ASP.NET Core Identity.
/// 
/// Flow:
/// 1. User enters email and password
/// 2. Form validates input (required fields, email format)
/// 3. Submit calls LoginCommand via MediatR
/// 4. Handler delegates to SignInManager.PasswordSignInAsync()
/// 5. Success: redirect to /dashboard; Error: display message
/// </summary>
public sealed partial class Login : ComponentBase
{
    [Inject] private IStringLocalizer<SharedResource> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;

    [SupplyParameterFromForm(FormName = "LoginForm")]
    private LoginFormModel LoginModel { get; set; } = new();
    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }
    // Toggle for password visibility (accessibility & usability)
    private bool ShowPassword { get; set; }
    private string PasswordInputType => ShowPassword ? "text" : "password";

    private async Task HandleLogin()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var command = new LoginCommand(LoginModel.Email, LoginModel.Password, LoginModel.RememberMe);
            var result = await Mediator.Send(command);

            if (result.Success)
            {
                Navigation.NavigateTo(result.RedirectUrl, replace: true);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed. Please try again.";
            }
        }
        catch (Exception)
        {
            ErrorMessage = "An unexpected error occurred. Please try again.";
            // Log exception in production
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void TogglePasswordVisibility()
    {
        ShowPassword = !ShowPassword;
    }

    private void ClearError()
    {
        ErrorMessage = null;
    }

    private sealed class LoginFormModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }
}
