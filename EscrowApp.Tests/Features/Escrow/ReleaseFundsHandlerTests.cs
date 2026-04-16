using EscrowApp.Features.Escrow.ReleaseFunds;

namespace EscrowApp.Tests.Features.Escrow;

/// <summary>
/// Unit tests for the ReleaseFunds MediatR handler.
/// Verifies: release of held funds, rejection when not held,
/// dispute blocks release, and correct Stripe capture call.
/// </summary>
public sealed class ReleaseFundsHandlerTests
{
    private readonly Mock<IEscrowTransactionRepository> _repositoryMock = new();
    private readonly Mock<IPaymentStrategyFactory> _strategyFactoryMock = new();
    private readonly Mock<IFundReleasable> _releaseStrategyMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private ReleaseFundsHandler CreateHandler()
    {
        return new ReleaseFundsHandler(_repositoryMock.Object, _strategyFactoryMock.Object);
    }

    private EscrowTransaction CreateHeldTransaction(int id = 1)
    {
        return new EscrowTransaction
        {
            Id = id,
            ClientEmail = "client@example.com",
            ConsultantEmail = "consultant@example.com",
            Amount = 1000.00m,
            ServiceDescription = "Web development",
            Status = "Funded (Held)",
            ExternalReference = "pi_test_12345",
            ExternalProvider = "Stripe",
            CreatedAt = DateTime.UtcNow
        };
    }

    private EscrowTransaction CreateDisputedTransaction(int id = 1)
    {
        return new EscrowTransaction
        {
            Id = id,
            ClientEmail = "client@example.com",
            ConsultantEmail = "consultant@example.com",
            Amount = 1000.00m,
            ServiceDescription = "Web development",
            Status = "Disputed",
            ExternalReference = "pi_test_12345",
            ExternalProvider = "Stripe",
            DisputeReason = "Service not completed",
            CreatedAt = DateTime.UtcNow
        };
    }

    private EscrowTransaction CreatePendingTransaction(int id = 1)
    {
        return new EscrowTransaction
        {
            Id = id,
            ClientEmail = "client@example.com",
            ConsultantEmail = "consultant@example.com",
            Amount = 1000.00m,
            ServiceDescription = "Web development",
            Status = "Pending",
            ExternalReference = null,
            ExternalProvider = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Handle_HeldTransaction_CapturesFundsAndReturnsSuccess()
    {
        // Arrange
        var transaction = CreateHeldTransaction(1);
        var command = new ReleaseFundsCommand(
            TransactionId: 1,
            IdempotencyKey: "idempotency-key-release-1");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(1, _ct))
            .ReturnsAsync(transaction);

        _strategyFactoryMock
            .Setup(f => f.ResolveReleaseStrategy("Stripe"))
            .Returns(_releaseStrategyMock.Object);

        _releaseStrategyMock
            .Setup(s => s.ReleaseFundsAsync("pi_test_12345", "release-1", _ct))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, _ct);

        // Assert
        result.Should().NotBeNull();
        result.TransactionId.Should().Be(1);
        result.Status.Should().Be("Completed (Released)");
        result.Success.Should().BeTrue();

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<EscrowTransaction>(), _ct), Times.Once);
        _releaseStrategyMock.Verify(
            s => s.ReleaseFundsAsync("pi_test_12345", "release-1", _ct),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DisputedTransaction_ThrowsInvalidOperationException()
    {
        // Arrange
        var transaction = CreateDisputedTransaction(2);
        var command = new ReleaseFundsCommand(
            TransactionId: 2,
            IdempotencyKey: "idempotency-key-release-2");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(2, _ct))
            .ReturnsAsync(transaction);

        var handler = CreateHandler();

        // Act & Assert
        await handler.Invoking(h => h.Handle(command, _ct))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Transaction 2 is disputed and cannot be released*");
    }

    [Fact]
    public async Task Handle_PendingTransaction_ThrowsInvalidOperationException()
    {
        // Arrange
        var transaction = CreatePendingTransaction(3);
        var command = new ReleaseFundsCommand(
            TransactionId: 3,
            IdempotencyKey: "idempotency-key-release-3");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(3, _ct))
            .ReturnsAsync(transaction);

        var handler = CreateHandler();

        // Act & Assert
        await handler.Invoking(h => h.Handle(command, _ct))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*must be in 'Funded (Held)' status*");
    }
}
