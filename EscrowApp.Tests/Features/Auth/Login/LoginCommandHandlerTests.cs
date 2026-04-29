using EscrowApp.Features.Auth.Login;
using EscrowApp.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace EscrowApp.Tests.Features.Auth.Login;

/// <summary>
/// Tests for LoginCommandHandler — validates credential checking via mocked SignInManager.
/// </summary>
public sealed class LoginCommandHandlerTests
{
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
            null!, null!, null!, null!);

        // Default: user has no roles → redirects to /dashboard/client
        var defaultUser = new ApplicationUser { Email = "user@example.com" };
        _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(defaultUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);

        _handler = new LoginCommandHandler(_signInManagerMock.Object, _userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccess()
    {
        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("user@example.com", "Pass123!", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var command = new LoginCommand("user@example.com", "Pass123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.RedirectUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_InvalidCredentials_ReturnsFailure()
    {
        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("user@example.com", "wrong", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var command = new LoginCommand("user@example.com", "wrong");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_LockedOut_ReturnsLockedOutMessage()
    {
        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("user@example.com", "Pass123!", false, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var command = new LoginCommand("user@example.com", "Pass123!");
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_RememberMe_PassedToSignInManager()
    {
        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync("user@example.com", "Pass123!", true, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var command = new LoginCommand("user@example.com", "Pass123!", RememberMe: true);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        _signInManagerMock.Verify(x => x.PasswordSignInAsync("user@example.com", "Pass123!", true, true), Times.Once);
    }
}
