using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using EscrowApp.Data;
using EscrowApp.Models;

namespace EscrowApp.Tests.Infrastructure.DependencyInjection;

public class IdentityDiRegistrationTests
{
    [Fact]
    public void DependencyContainer_RegistersIdentityServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<EscrowDbContext>(options =>
            options.UseInMemoryDatabase("test_identity_di"));
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddIdentity<ApplicationUser, IdentityRole<int>>()
            .AddEntityFrameworkStores<EscrowDbContext>();

        var sp = services.BuildServiceProvider();

        // Act & Assert
        // Verify key Identity services are registered
        sp.GetService<UserManager<ApplicationUser>>().Should().NotBeNull();
        sp.GetService<RoleManager<IdentityRole<int>>>().Should().NotBeNull();
        sp.GetService<SignInManager<ApplicationUser>>().Should().NotBeNull();
    }

    [Fact]
    public void DependencyContainer_RegistersPasswordHasher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<EscrowDbContext>(options =>
            options.UseInMemoryDatabase("test_di_hasher"));
        services.AddIdentity<ApplicationUser, IdentityRole<int>>()
            .AddEntityFrameworkStores<EscrowDbContext>();

        var sp = services.BuildServiceProvider();

        // Act
        var hasher = sp.GetService<IPasswordHasher<ApplicationUser>>();

        // Assert
        hasher.Should().NotBeNull("IPasswordHasher should be registered");
    }

    [Fact]
    public void PasswordHasher_CanHashPassword()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<EscrowDbContext>(options =>
            options.UseInMemoryDatabase("test_di_hash"));
        services.AddIdentity<ApplicationUser, IdentityRole<int>>()
            .AddEntityFrameworkStores<EscrowDbContext>();

        var sp = services.BuildServiceProvider();
        var hasher = sp.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "testuser" };
        var password = "SecurePassword123!";

        // Act
        var hashedPassword = hasher.HashPassword(user, password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
    }

    [Fact]
    public void PasswordHasher_CanVerifyPassword()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<EscrowDbContext>(options =>
            options.UseInMemoryDatabase("test_di_verify"));
        services.AddIdentity<ApplicationUser, IdentityRole<int>>()
            .AddEntityFrameworkStores<EscrowDbContext>();

        var sp = services.BuildServiceProvider();
        var hasher = sp.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "testuser" };
        var password = "SecurePassword123!";
        var hashedPassword = hasher.HashPassword(user, password);

        // Act
        var verificationResult = hasher.VerifyHashedPassword(user, hashedPassword, password);

        // Assert
        verificationResult.Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void PasswordHasher_RejectsWrongPassword()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<EscrowDbContext>(options =>
            options.UseInMemoryDatabase("test_di_wrong"));
        services.AddIdentity<ApplicationUser, IdentityRole<int>>()
            .AddEntityFrameworkStores<EscrowDbContext>();

        var sp = services.BuildServiceProvider();
        var hasher = sp.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "testuser" };
        var password = "SecurePassword123!";
        var hashedPassword = hasher.HashPassword(user, password);

        // Act
        var verificationResult = hasher.VerifyHashedPassword(user, hashedPassword, "WrongPassword456!");

        // Assert
        verificationResult.Should().Be(PasswordVerificationResult.Failed);
    }
}


