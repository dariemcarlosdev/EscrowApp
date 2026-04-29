using MediatR;

namespace EscrowApp.Features.Auth.Register;

/// <summary>
/// Register command — creates a new user via ASP.NET Core Identity.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string DisplayName,
    string Role) : IRequest<RegisterResult>;

/// <summary>
/// Result of registration operation.
/// </summary>
public sealed record RegisterResult(bool Success, string? ErrorMessage = null)
{
    public static RegisterResult SuccessResult() => new(true);
    public static RegisterResult FailureResult(string message) => new(false, message);
}
