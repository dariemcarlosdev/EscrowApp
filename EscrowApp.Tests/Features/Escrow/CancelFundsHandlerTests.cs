using EscrowApp.Features.Escrow.CancelFunds;

namespace EscrowApp.Tests.Features.Escrow;

/// <summary>
/// Unit tests for the CancelFunds MediatR handler.
/// Verifies: voluntary cancellation of held funds, rejection when
/// not in cancellable state, idempotency key usage, and Stripe void call.
/// </summary>
public sealed class CancelFundsHandlerTests
{
    private readonly Mock<IEscrowTransactionRepository> _repositoryMock = new();
    private readonly Mock<IPaymentStrategyFactory> _strategyFactoryMock = new();
    private readonly Mock<IFundCancellable> _cancelStrategyMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private CancelFundsHandler CreateHandler()
    {
        return new CancelFundsHandler(_repositoryMock.Object, _strategyFactoryMock.Object, _eventBusMock.Object);
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

    private EscrowTransaction CreateReleasedTransaction(int id = 1)
    {
        return new EscrowTransaction
        {
            Id = id,
            ClientEmail = "client@example.com",
            ConsultantEmail = "consultant@example.com",
            Amount = 1000.00m,
            ServiceDescription = "Web development",
            Status = "Completed (Released)",
            ExternalReference = "pi_test_12345",
            ExternalProvider = "Stripe",
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Handle_HeldTransaction_CancelsHoldAndReturnsSuccess()
    {
        // Arrange
        var transaction = CreateHeldTransaction(1);
        var command = new CancelFundsCommand(
            TransactionId: 1,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel-1");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(1, _ct))
            .ReturnsAsync(transaction);

        _strategyFactoryMock
            .Setup(f => f.ResolveCancelStrategy("Stripe"))
            .Returns(_cancelStrategyMock.Object);

        _cancelStrategyMock
            .Setup(s => s.CancelHoldAsync("pi_test_12345", "idempotency-key-cancel-1", _ct))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, _ct);

        // Assert
        result.Should().NotBeNull();
        result.TransactionId.Should().Be(1);
        result.Status.Should().Be("Cancelled");
        result.ExternalReference.Should().Be("pi_test_12345");
        result.ExternalProvider.Should().Be("Stripe");
        result.Reason.Should().Be("Mutual agreement to cancel");
        result.CancelledBy.Should().Be("client@example.com");

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<EscrowTransaction>(), _ct), Times.Once);
        _cancelStrategyMock.Verify(
            s => s.CancelHoldAsync("pi_test_12345", "idempotency-key-cancel-1", _ct),
            Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<FundsCancelledEvent>(), _ct), Times.Once);
    }

    [Fact]
    public async Task Handle_DisputedTransaction_ThrowsInvalidOperationException()
    {
        // Arrange
        var transaction = CreateDisputedTransaction(2);
        var command = new CancelFundsCommand(
            TransactionId: 2,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel-2");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(2, _ct))
            .ReturnsAsync(transaction);

        var handler = CreateHandler();

        // Act & Assert
        await handler.Invoking(h => h.Handle(command, _ct))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel transaction in status 'Disputed'*");
    }

    [Fact]
    public async Task Handle_ReleasedTransaction_ThrowsInvalidOperationException()
    {
        // Arrange
        var transaction = CreateReleasedTransaction(3);
        var command = new CancelFundsCommand(
            TransactionId: 3,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel-3");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(3, _ct))
            .ReturnsAsync(transaction);

        var handler = CreateHandler();

        // Act & Assert
        await handler.Invoking(h => h.Handle(command, _ct))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel transaction in status 'Completed (Released)'*");
    }

    [Fact]
    public async Task Handle_StripeCancelFails_StillUpdatesStatusAndPublishesEvent()
    {
        // Arrange
        var transaction = CreateHeldTransaction(4);
        var command = new CancelFundsCommand(
            TransactionId: 4,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel-4");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(4, _ct))
            .ReturnsAsync(transaction);

        _strategyFactoryMock
            .Setup(f => f.ResolveCancelStrategy("Stripe"))
            .Returns(_cancelStrategyMock.Object);

        // Mock cancel to return false (Stripe rejection)
        _cancelStrategyMock
            .Setup(s => s.CancelHoldAsync("pi_test_12345", "idempotency-key-cancel-4", _ct))
            .ReturnsAsync(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, _ct);

        // Assert — handler updates status and publishes event regardless of Stripe success
        // This is the actual behavior per the handler implementation (lines 65-77)
        result.Should().NotBeNull();
        result.Status.Should().Be("Cancelled");
        
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<EscrowTransaction>(), _ct), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<FundsCancelledEvent>(), _ct), Times.Once);
    }
}
