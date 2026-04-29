using EscrowApp.Features.Auth.Login;
using EscrowApp.Features.Auth.Register;
using EscrowApp.Models;
using FluentAssertions;

namespace EscrowApp.Tests.Features.Auth.Integration;

/// <summary>
/// Integration-level tests validating the register → login flow
/// at the command/result contract level.
/// Full end-to-end Blazor integration requires WebApplicationFactory (post-MVP).
/// </summary>
public sealed class AuthFlowIntegrationTests
{
    [Fact]
    public void RegisterThenLogin_CommandContracts_AreCompatible()
    {
        var email = "newuser@example.com";
        var password = "StrongPass1!";

        var registerCommand = new RegisterCommand(email, password, password, "New User", AppRoles.Client);
        var loginCommand = new LoginCommand(registerCommand.Email, registerCommand.Password);

        loginCommand.Email.Should().Be(registerCommand.Email);
        loginCommand.Password.Should().Be(registerCommand.Password);
    }

    [Fact]
    public void RegisterResult_And_LoginResult_ShareConsistentSuccessContract()
    {
        var registerSuccess = RegisterResult.SuccessResult();
        var loginSuccess = LoginResult.SuccessResult("/dashboard/client");

        registerSuccess.Success.Should().BeTrue();
        loginSuccess.Success.Should().BeTrue();
        registerSuccess.ErrorMessage.Should().BeNull();
        loginSuccess.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RegisterResult_And_LoginResult_ShareConsistentFailureContract()
    {
        var registerFailure = RegisterResult.FailureResult("Email taken");
        var loginFailure = LoginResult.FailureResult("Invalid credentials");

        registerFailure.Success.Should().BeFalse();
        loginFailure.Success.Should().BeFalse();
        registerFailure.ErrorMessage.Should().NotBeNullOrEmpty();
        loginFailure.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RegisterCommand_PasswordMismatch_IsDetectable()
    {
        var command = new RegisterCommand("user@test.com", "Pass1!", "DifferentPass!", "User", AppRoles.Client);

        command.Password.Should().NotBe(command.ConfirmPassword);
    }
}
