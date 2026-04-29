using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using EscrowApp.Features.Auth.Register;
using EscrowApp.Models;
using EscrowApp.Data;

namespace EscrowApp.Tests.Features.Auth.Register;

/// <summary>
/// Tests for RegisterCommandHandler — creates ApplicationUser + Actor bridge via UserManager.
/// Source: EscrowApp/Features/Auth/Register/RegisterCommandHandler.cs
/// Framework: xUnit + Moq + FluentAssertions
/// 
/// Testing strategy:
/// - Uses REAL EscrowDbContext with Sqlite in-memory database (supports transactions)
/// - Mocks UserManager<ApplicationUser> with callback behavior to simulate persistence
/// - Tests both successful Actor+ApplicationUser creation and failure scenarios with rollback
/// - Verifies transactional integrity: on UserManager failure, Actor should not persist
/// </summary>
public sealed class RegisterCommandHandlerTests : IAsyncLifetime
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private EscrowDbContext _dbContext = null!;
    private RegisterCommandHandler _sut = null!;
    private SqliteConnection? _sqliteConnection;

    /// <summary>
    /// Initialize Sqlite in-memory database for each test.
    /// Sqlite in-memory supports transactions (unlike EF Core's in-memory provider).
    /// Uses shared SqliteConnection to ensure schema persists across queries.
    /// IAsyncLifetime ensures Dispose runs after test completes.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create a shared SQLite connection - keeps in-memory database alive
        _sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await _sqliteConnection.OpenAsync();

        // Use SQLite with shared connection which supports transactions
        var options = new DbContextOptionsBuilder<EscrowDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;

        _dbContext = new EscrowDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        
        // Create UserManager mock with callback behavior
        _userManagerMock = CreateUserManagerMock();
        
        _sut = new RegisterCommandHandler(_userManagerMock.Object, _dbContext);
    }

    /// <summary>
    /// Clean up database and connection after each test.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        if (_sqliteConnection != null)
        {
            await _sqliteConnection.CloseAsync();
            await _sqliteConnection.DisposeAsync();
        }
    }

    /// <summary>
    /// Create a properly configured UserManager mock.
    /// Callback behavior simulates real persistence: successful CreateAsync saves user to DbContext.
    /// </summary>
    private Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mock = new Mock<UserManager<ApplicationUser>>(
            store.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());

        // Default setup: CreateAsync saves user to DbContext on success
        mock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Returns<ApplicationUser, string>(async (user, password) =>
            {
                // Simulate successful user creation: add to DbContext
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
                return IdentityResult.Success;
            });

        // Default setup: AddToRoleAsync succeeds
        mock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        return mock;
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WhenValidRegistration_ShouldCreateActorAndUserAndReturnSuccess()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            DisplayName: "John Doe",
            Role: AppRoles.Client
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        
        // Verify Actor was created in the database
        var actor = await _dbContext.Actors.FirstOrDefaultAsync(a => a.DisplayName == "John Doe");
        actor.Should().NotBeNull();
        actor!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Verify User was created with ActorId bridge
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == command.Email);
        user.Should().NotBeNull();
        user!.ActorId.Should().Be(actor.Id);
        
        // Verify UserManager was called with correct ActorId
        _userManagerMock.Verify(um => um.CreateAsync(
            It.Is<ApplicationUser>(u => 
                u.Email == command.Email && 
                u.UserName == command.Email &&
                u.ActorId == actor.Id),
            command.Password), Times.Once);

        // Verify role was assigned
        _userManagerMock.Verify(um => um.AddToRoleAsync(
            It.IsAny<ApplicationUser>(), AppRoles.Client), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserManagerSucceeds_ShouldCreateBridgeAndReturnSuccess()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        // Verify Actor exists in database
        var actor = await _dbContext.Actors.FirstOrDefaultAsync(a => a.DisplayName == command.DisplayName);
        actor.Should().NotBeNull("Actor should be created and persisted via transaction");
        
        // Verify User exists with ActorId FK
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == command.Email);
        user.Should().NotBeNull();
        user!.ActorId.Should().Be(actor!.Id);
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [InlineData("", "SecurePass123!", "SecurePass123!", "John Doe")]
    [InlineData("user@example.com", "", "", "John Doe")]
    [InlineData("user@example.com", "SecurePass123!", "SecurePass123!", "")]
    public async Task Handle_WhenRequiredFieldsEmpty_ShouldStillProcessToUserManager(
        string email, string password, string confirmPassword, string displayName)
    {
        // Arrange - Handler doesn't validate empty fields, delegates to UserManager
        var command = new RegisterCommand(email, password, confirmPassword, displayName, AppRoles.Client);
        
        // Override default mock: UserManager will fail for empty/invalid fields
        _userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidUserName", Description = "Username is required" }));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Username is required");
        
        // Verify Actor was NOT persisted (transaction rolled back on failure)
        var actorCount = await _dbContext.Actors.CountAsync();
        actorCount.Should().Be(0, "Transaction should be rolled back when UserManager fails");
    }

    [Fact]
    public async Task Handle_WhenPasswordsDoNotMatch_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterCommand(
            Email: "user@example.com",
            Password: "SecurePass123!",
            ConfirmPassword: "DifferentPass456!",
            DisplayName: "John Doe",
            Role: AppRoles.Client
        );

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Passwords do not match.");
        
        // Should not call UserManager when passwords don't match
        _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
        
        // Actor should not be created
        var actorCount = await _dbContext.Actors.CountAsync();
        actorCount.Should().Be(0);
    }

    [Theory]
    [InlineData("", "")]  // Both empty passwords match, so will proceed to UserManager
    public async Task Handle_WhenPasswordsAreEmptyButMatch_ShouldProcessToUserManager(string password, string confirmPassword)
    {
        // Arrange - Handler doesn't validate empty fields, delegates to UserManager
        var command = new RegisterCommand("user@example.com", password, confirmPassword, "John Doe", AppRoles.Client);
        
        // UserManager will fail for empty passwords
        _userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordRequiredError", Description = "Password is required" }));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Password is required");
        
        // Actor should not be persisted (transaction rolled back)
        var actorCount = await _dbContext.Actors.CountAsync();
        actorCount.Should().Be(0);
    }

    [Theory]
    [InlineData("", "SecurePass123!")]
    [InlineData("SecurePass123!", "")]
    public async Task Handle_WhenPasswordsDoNotMatch_ShouldReturnFailureBeforeUserManager(string password, string confirmPassword)
    {
        // Arrange
        var command = new RegisterCommand("user@example.com", password, confirmPassword, "John Doe", AppRoles.Client);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Passwords do not match.");
        
        // UserManager should not be called when passwords don't match
        _userManagerMock.Verify(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        
        // No Actor should be created
        var actorCount = await _dbContext.Actors.CountAsync();
        actorCount.Should().Be(0);
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public async Task Handle_WhenUserManagerFails_ShouldRollbackActorAndReturnFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        var identityErrors = new[]
        {
            new IdentityError { Code = "DuplicateUserName", Description = "Username 'user@example.com' is already taken." },
            new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 6 characters." }
        };
        
        // Override default mock: UserManager will fail with multiple errors
        _userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Username 'user@example.com' is already taken.");
        result.ErrorMessage.Should().Contain("Passwords must be at least 6 characters.");

        // Verify Actor was NOT persisted (rolled back) — no Actor in database
        var actorCount = await _dbContext.Actors.CountAsync();
        actorCount.Should().Be(0, "Transaction should have been rolled back, Actor should not persist");
    }

    [Fact]
    public async Task Handle_WhenUserManagerThrowsException_ShouldRollbackAndReturnFailureResult()
    {
        // Arrange
        var command = CreateValidCommand();
        
        // Override default mock: UserManager throws exception
        _userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — handler catches exception and returns failure result (transaction rollback)
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Registration failed");
        result.ErrorMessage.Should().Contain("Database connection failed");

        // Verify Actor was NOT persisted (rolled back via exception handler)
        var actorCount = await _dbContext.Actors.CountAsync();
        actorCount.Should().Be(0, "Exception should trigger rollback, Actor should not persist");
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnFailureWithDuplicateErrorAndRollback()
    {
        // Arrange
        var command = CreateValidCommand();
        var duplicateError = new IdentityError 
        { 
            Code = "DuplicateEmail", 
            Description = "Email is already registered." 
        };
        
        // Override default mock: UserManager rejects duplicate email
        _userManagerMock
            .Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(duplicateError));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email is already registered.");

        // Verify Actor was NOT persisted (transaction rolled back)
        var actorCount = await _dbContext.Actors.CountAsync();
        actorCount.Should().Be(0, "Transaction should have been rolled back on UserManager failure");
    }

    #endregion

    #region Role Assignment Tests

    [Theory]
    [InlineData(AppRoles.Client)]
    [InlineData(AppRoles.Consultant)]
    public async Task Handle_WhenValidRole_ShouldAssignRoleAndReturnSuccess(string role)
    {
        var command = CreateValidCommand(role);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        _userManagerMock.Verify(um => um.AddToRoleAsync(
            It.IsAny<ApplicationUser>(), role), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvalidRole_ShouldReturnFailureWithoutCallingUserManager()
    {
        var command = CreateValidCommand("InvalidRole");
        var result = await _sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid role");
        _userManagerMock.Verify(um => um.CreateAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoleAssignmentFails_ShouldRollbackAndReturnFailure()
    {
        _userManagerMock
            .Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "RoleNotFound", Description = "Role 'Client' does not exist." }));

        var command = CreateValidCommand(AppRoles.Client);
        var result = await _sut.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Role assignment failed");

        var actorCount = await _dbContext.Actors.CountAsync();
        actorCount.Should().Be(0, "Transaction should be rolled back when role assignment fails");
    }

    #endregion

    #region Helper Methods

    private static RegisterCommand CreateValidCommand(string role = AppRoles.Client) => new(
        Email: "user@example.com",
        Password: "SecurePass123!",
        ConfirmPassword: "SecurePass123!",
        DisplayName: "John Doe",
        Role: role
    );

    #endregion
}
