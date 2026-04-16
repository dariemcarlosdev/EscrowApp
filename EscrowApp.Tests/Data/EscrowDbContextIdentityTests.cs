using EscrowApp.Data;
using EscrowApp.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EscrowApp.Tests.Data;

/// <summary>
/// Unit tests for EscrowDbContext Identity configuration.
/// Tests that ApplicationUser (Identity) is properly configured in the DbContext.
/// </summary>
public sealed class EscrowDbContextIdentityTests
{
    [Fact]
    public void DbContext_HasApplicationUserDbSet()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("IdentityTest")
            .Options;

        // Act
        using var context = new EscrowDbContext(options);

        // Assert: Verify DbSet<ApplicationUser> exists
        context.Model.FindEntityType(typeof(ApplicationUser)).Should().NotBeNull();
    }

    [Fact]
    public void DbContext_HasIdentityRoleDbSet()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("IdentityRoleTest")
            .Options;

        // Act
        using var context = new EscrowDbContext(options);

        // Assert: Verify DbSet<IdentityRole<int>> exists
        context.Model.FindEntityType(typeof(IdentityRole<int>)).Should().NotBeNull();
    }

    [Fact]
    public void ApplicationUser_HasActorForeignKey()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("ActorFKTest")
            .Options;

        using var context = new EscrowDbContext(options);

        // Act & Assert: Verify navigation property exists
        var appUserType = context.Model.FindEntityType(typeof(ApplicationUser));
        appUserType.Should().NotBeNull();
        
        var navigation = appUserType!.FindNavigation("Actor");
        navigation.Should().NotBeNull();
    }

    [Fact]
    public void ApplicationUser_EmailIsUnique()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("EmailUniqueTest")
            .Options;

        using var context = new EscrowDbContext(options);

        // Act & Assert: Verify NormalizedEmail index exists
        var appUserType = context.Model.FindEntityType(typeof(ApplicationUser));
        var emailIndex = appUserType!.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == "NormalizedEmail"));
        
        emailIndex.Should().NotBeNull("Email should have a unique index via Identity configuration");
    }

    [Fact]
    public void ApplicationUser_UserNameIsUnique()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("UserNameUniqueTest")
            .Options;

        using var context = new EscrowDbContext(options);

        // Act & Assert: Verify NormalizedUserName index exists
        var appUserType = context.Model.FindEntityType(typeof(ApplicationUser));
        var userNameIndex = appUserType!.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == "NormalizedUserName"));
        
        userNameIndex.Should().NotBeNull("UserName should have a unique index via Identity configuration");
    }
}
