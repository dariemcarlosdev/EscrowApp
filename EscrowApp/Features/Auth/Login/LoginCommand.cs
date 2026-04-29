using MediatR;

namespace EscrowApp.Features.Auth.Login;

/// <summary>
/// Login command — orchestrates ASP.NET Core Identity authentication.
/// </summary>
public sealed record LoginCommand(string Email, string Password, bool RememberMe = false) : IRequest<LoginResult>;

/// <summary>
/// Result of login operation.
/// </summary>
public sealed record LoginResult(bool Success, string? ErrorMessage = null, string RedirectUrl = "/")
{
    public static LoginResult SuccessResult(string redirectUrl) => new(true, null, redirectUrl);
    public static LoginResult FailureResult(string message) => new(false, message);
}
