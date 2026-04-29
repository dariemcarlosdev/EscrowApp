using EscrowApp.Features.Auth.Login;
using FluentAssertions;

namespace EscrowApp.Tests.Features.Auth.Login;

/// <summary>
/// Tests for LoginCommand and LoginResult — validates command/result structure.
/// 
/// Note: Full LoginCommandHandler testing requires ASP.NET Core integration tests
/// with a real or in-memory database and SignInManager<ApplicationUser> instance,
/// which can't be mocked directly due to lack of parameterless constructor.
/// </summary>
public sealed class LoginCommandTests
{
    [Fact]
    public void LoginCommand_WithValidEmail_CreatesCommand()
    {
        // Arrange
        var email = "user@example.com";
        var password = "ValidPassword123";
        var rememberMe = true;

        // Act
        var command = new LoginCommand(email, password, rememberMe);

        // Assert
        command.Email.Should().Be(email);
        command.Password.Should().Be(password);
        command.RememberMe.Should().BeTrue();
    }

    [Fact]
    public void LoginCommand_DefaultRememberMe_IsFalse()
    {
        // Arrange & Act
        var command = new LoginCommand("user@example.com", "Password123");

        // Assert
        command.RememberMe.Should().BeFalse();
    }

    [Fact]
    public void LoginResult_SuccessResult_HasSuccess()
    {
        // Act
        var result = LoginResult.SuccessResult("/dashboard/client");

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void LoginResult_FailureResult_HasErrorMessage()
    {
        // Arrange
        var errorMessage = "Invalid credentials";

        // Act
        var result = LoginResult.FailureResult(errorMessage);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
    }
}
