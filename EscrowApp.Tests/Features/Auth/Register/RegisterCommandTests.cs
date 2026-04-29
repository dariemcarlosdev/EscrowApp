using EscrowApp.Features.Auth.Register;
using EscrowApp.Models;
using FluentAssertions;

namespace EscrowApp.Tests.Features.Auth.Register;

/// <summary>
/// Tests for RegisterCommand and RegisterResult — validates command/result structure.
/// Handler tests are in RegisterCommandHandlerTests.cs.
/// </summary>
public sealed class RegisterCommandTests
{
    [Fact]
    public void RegisterCommand_CreatesWithAllProperties()
    {
        var command = new RegisterCommand("user@example.com", "Pass123!", "Pass123!", "Test User", AppRoles.Client);

        command.Email.Should().Be("user@example.com");
        command.Password.Should().Be("Pass123!");
        command.ConfirmPassword.Should().Be("Pass123!");
        command.DisplayName.Should().Be("Test User");
        command.Role.Should().Be(AppRoles.Client);
    }

    [Theory]
    [InlineData(AppRoles.Client)]
    [InlineData(AppRoles.Consultant)]
    public void RegisterCommand_AcceptsBothValidRoles(string role)
    {
        var command = new RegisterCommand("user@example.com", "Pass123!", "Pass123!", "Test User", role);

        AppRoles.All.Should().Contain(command.Role);
    }

    [Fact]
    public void RegisterResult_SuccessResult_HasSuccess()
    {
        var result = RegisterResult.SuccessResult();

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RegisterResult_FailureResult_HasErrorMessage()
    {
        var result = RegisterResult.FailureResult("Email already taken.");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email already taken.");
    }
}
