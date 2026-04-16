using EscrowApp.Features.Escrow.HoldFunds;

namespace EscrowApp.Tests.Features.Escrow;

/// <summary>
/// Unit tests for the HoldFunds MediatR handler.
/// Verifies: hold on pending transaction, rejection of invalid states,
/// idempotency key propagation, and domain event publishing.
/// </summary>
public sealed class HoldFundsHandlerTests
{
    private readonly Mock<IEscrowTransactionRepository> _repositoryMock = new();
    private readonly Mock<IPaymentStrategyFactory> _strategyFactoryMock = new();
    private readonly Mock<IFundHoldable> _holdStrategyMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private HoldFundsHandler CreateHandler()
    {
        return new HoldFundsHandler(_repositoryMock.Object, _strategyFactoryMock.Object, _eventBusMock.Object);
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

    [Fact]
    public async Task Handle_ValidPendingTransaction_HoldsFundsAndReturnsSuccess()
    {
        // Arrange
        var transaction = CreatePendingTransaction(1);
        var command = new HoldFundsCommand(
            TransactionId: 1,
            PaymentMethodId: "pm_test_456",
            IdempotencyKey: "idempotency-key-hold-1",
            ProviderName: "Stripe");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(1, _ct))
            .ReturnsAsync(transaction);

        _strategyFactoryMock
            .Setup(f => f.ResolveHoldStrategy("Stripe"))
            .Returns(_holdStrategyMock.Object);

        _holdStrategyMock
            .Setup(s => s.HoldFundsAsync(1000.00m, "pm_test_456", "hold-1", _ct))
            .ReturnsAsync("pi_test_12345");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, _ct);

        // Assert
        result.Should().NotBeNull();
        result.TransactionId.Should().Be(1);
        result.Status.Should().Be("Funded (Held)");
        result.ExternalReference.Should().Be("pi_test_12345");
        result.ExternalProvider.Should().Be("Stripe");
        result.Amount.Should().Be(1000.00m);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<EscrowTransaction>(), _ct), Times.Once);
        _eventBusMock.Verify(e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), _ct), Times.Once);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new HoldFundsCommand(
            TransactionId: 999,
            PaymentMethodId: "pm_test_456",
            IdempotencyKey: "idempotency-key-hold-999",
            ProviderName: "Stripe");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(999, _ct))
            .ReturnsAsync((EscrowTransaction)null!);

        var handler = CreateHandler();

        // Act & Assert
        await handler.Invoking(h => h.Handle(command, _ct))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Transaction 999 not found*");
    }

    [Fact]
    public async Task Handle_StripeHoldSucceeds_PublishesPaymentReceivedEvent()
    {
        // Arrange
        var transaction = CreatePendingTransaction(2);
        var command = new HoldFundsCommand(
            TransactionId: 2,
            PaymentMethodId: "pm_test_789",
            IdempotencyKey: "idempotency-key-hold-2",
            ProviderName: "Stripe");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(2, _ct))
            .ReturnsAsync(transaction);

        _strategyFactoryMock
            .Setup(f => f.ResolveHoldStrategy("Stripe"))
            .Returns(_holdStrategyMock.Object);

        _holdStrategyMock
            .Setup(s => s.HoldFundsAsync(1000.00m, "pm_test_789", "hold-2", _ct))
            .ReturnsAsync("pi_test_67890");

        var handler = CreateHandler();

        // Act
        await handler.Handle(command, _ct);

        // Assert
        _eventBusMock.Verify(
            e => e.PublishAsync(
                It.Is<PaymentReceivedEvent>(evt =>
                    evt.TransactionId == 2 &&
                    evt.Amount == 1000.00m &&
                    evt.ExternalReference == "pi_test_67890" &&
                    evt.Provider == "Stripe"),
                _ct),
            Times.Once);
    }
}
