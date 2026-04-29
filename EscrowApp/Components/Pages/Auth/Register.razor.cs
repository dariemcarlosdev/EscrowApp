using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MediatR;
using EscrowApp.Features.Auth.Register;
using EscrowApp.Models;
using System.Linq;

namespace EscrowApp.Components.Pages.Auth;

/// <summary>
/// Registration page — new user signup via ASP.NET Core Identity.
///
/// Flow:
/// 1. User selects role (Client / Consultant), enters display name, email, password
/// 2. Form validates input (required fields, email format, password match, role selected)
/// 3. Submit calls RegisterCommand via MediatR
/// 4. Handler creates Actor → ApplicationUser → assigns role atomically
/// 5. Success: redirect to /auth/login; Error: display message
/// </summary>
public sealed partial class Register : ComponentBase
{
    [Inject] private IStringLocalizer<SharedResource> L { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;

    [SupplyParameterFromForm(FormName = "RegisterForm")]
    private RegisterFormModel RegisterModel { get; set; } = new();

    private string? ErrorMessage { get; set; }
    private bool IsLoading { get; set; }
    private bool ShowRoleError { get; set; }

    // Password visibility & strength (UX improvements)
    private bool ShowPassword { get; set; }
    private bool ShowConfirmPassword { get; set; }
    private int PasswordStrength { get; set; }
    private string PasswordInputType => ShowPassword ? "text" : "password";
    private string ConfirmPasswordInputType => ShowConfirmPassword ? "text" : "password";

    private async Task HandleRegister()
    {
        ShowRoleError = false;

        if (string.IsNullOrEmpty(RegisterModel.Role))
        {
            ShowRoleError = true;
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var command = new RegisterCommand(
                RegisterModel.Email,
                RegisterModel.Password,
                RegisterModel.ConfirmPassword,
                RegisterModel.DisplayName,
                RegisterModel.Role);

            var result = await Mediator.Send(command);

            if (result.Success)
            {
                Navigation.NavigateTo("/auth/login", replace: true);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Registration failed. Please try again.";
            }
        }
        catch (Exception)
        {
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdatePasswordStrength(string pwd)
    {
        if (string.IsNullOrEmpty(pwd))
        {
            PasswordStrength = 0;
            return;
        }

        int score = Math.Min(100, pwd.Length * 8);
        if (pwd.Any(char.IsUpper)) score += 8;
        if (pwd.Any(char.IsDigit)) score += 8;
        if (pwd.Any(ch => !char.IsLetterOrDigit(ch))) score += 8;
        PasswordStrength = Math.Clamp(score, 0, 100);
    }

    private void TogglePasswordVisibility()
    {
        ShowPassword = !ShowPassword;
    }

    private void ToggleConfirmPasswordVisibility()
    {
        ShowConfirmPassword = !ShowConfirmPassword;
    }

    private void ClearError()
    {
        ErrorMessage = null;
        ShowRoleError = false;
    }

    private sealed class RegisterFormModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
