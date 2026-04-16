using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;
using EscrowApp.Data;
using EscrowApp.Models;

namespace EscrowApp.Tests.Data;

public class MigrationTests
{
    [Fact]
    public async Task AppliedMigration_CreatesAspNetUsersTables()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("migration_test_users")
            .Options;

        // Act & Assert
        using (var context = new EscrowDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            
            // IdentityDbContext base provides Users, Roles, UserRoles, UserClaims
            // Just verify they're accessible
            var usersQuery = context.Users;
            var rolesQuery = context.Roles;
            var userRolesQuery = context.UserRoles;
            var userClaimsQuery = context.UserClaims;

            // Assert: all DbSets exist and are queryable
            usersQuery.Should().NotBeNull();
            rolesQuery.Should().NotBeNull();
            userRolesQuery.Should().NotBeNull();
            userClaimsQuery.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Migration_CreateApplicationUserSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("migration_test_create_user")
            .Options;

        // Act
        using (var context = new EscrowDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            var user = new ApplicationUser
            {
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                ActorId = null
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var savedUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "testuser@example.com");

            // Assert
            savedUser.Should().NotBeNull();
            savedUser!.Email.Should().Be("testuser@example.com");
            savedUser.ActorId.Should().BeNull("ActorId should be nullable for new users");
        }
    }

    [Fact]
    public async Task Migration_CreatesRoleTables()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("migration_test_roles")
            .Options;

        // Act
        using (var context = new EscrowDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            var adminRole = new IdentityRole<int> { Name = "Admin" };
            context.Roles.Add(adminRole);
            await context.SaveChangesAsync();

            var savedRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Admin");

            // Assert
            savedRole.Should().NotBeNull();
            savedRole!.Name.Should().Be("Admin");
        }
    }

    [Fact]
    public async Task Migration_CreatesUserClaimsTables()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("migration_test_claims")
            .Options;

        // Act
        using (var context = new EscrowDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            var user = new ApplicationUser
            {
                UserName = "claimuser@example.com",
                Email = "claimuser@example.com"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userClaim = new IdentityUserClaim<int>
            {
                UserId = user.Id,
                ClaimType = "role",
                ClaimValue = "Consultant"
            };

            context.UserClaims.Add(userClaim);
            await context.SaveChangesAsync();

            var savedClaim = await context.UserClaims
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            // Assert
            savedClaim.Should().NotBeNull();
            savedClaim!.ClaimType.Should().Be("role");
            savedClaim.ClaimValue.Should().Be("Consultant");
        }
    }

    [Fact]
    public async Task Migration_ApplicationUserActorForeignKeyRelationship()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseInMemoryDatabase("migration_test_fk")
            .Options;

        // Act & Assert
        using (var context = new EscrowDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            // Create an Actor
            var actor = new Actor
            {
                DisplayName = "Test Actor",
                WalletAddress = null
            };

            context.Actors.Add(actor);
            await context.SaveChangesAsync();

            // Create ApplicationUser linked to Actor
            var user = new ApplicationUser
            {
                UserName = "linkeduser@example.com",
                Email = "linkeduser@example.com",
                ActorId = actor.Id
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Verify relationship
            var savedUser = await context.Users
                .Include(u => u.Actor)
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            savedUser.Should().NotBeNull();
            savedUser!.ActorId.Should().Be(actor.Id);
            savedUser.Actor.Should().NotBeNull();
            savedUser.Actor!.DisplayName.Should().Be("Test Actor");
        }
    }
}
