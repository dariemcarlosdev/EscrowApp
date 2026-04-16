using EscrowApp.Features.Escrow.DisputeFunds;

namespace EscrowApp.Tests.Features.Escrow;

/// <summary>
/// Unit tests for the DisputeFunds MediatR handler.
/// Verifies: dispute raised on held transaction, rejection of
/// non-held states, and DisputeRaisedEvent publication.
/// </summary>
public sealed class DisputeFundsHandlerTests
{
    private readonly Mock<IEscrowTransactionRepository> _repositoryMock = new();
    private readonly Mock<IPaymentStrategyFactory> _strategyFactoryMock = new();
    private readonly Mock<IFundCancellable> _cancelStrategyMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private DisputeFundsHandler CreateHandler()
    {
        return new DisputeFundsHandler(_repositoryMock.Object, _strategyFactoryMock.Object, _eventBusMock.Object);
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
    public async Task Handle_HeldTransaction_SetsDisputedStatusAndPublishesEvent()
    {
        // Arrange
        var transaction = CreateHeldTransaction(1);
        var command = new DisputeFundsCommand(
            TransactionId: 1,
            Reason: "Service was not completed as agreed.",
            RaisedBy: "client@example.com",
            IdempotencyKey: "idempotency-key-dispute-1");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(1, _ct))
            .ReturnsAsync(transaction);

        _strategyFactoryMock
            .Setup(f => f.ResolveCancelStrategy("Stripe"))
            .Returns(_cancelStrategyMock.Object);

        _cancelStrategyMock
            .Setup(s => s.CancelHoldAsync("pi_test_12345", "dispute-1", _ct))
            .ReturnsAsync(true);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, _ct);

        // Assert
        result.Should().NotBeNull();
        result.TransactionId.Should().Be(1);
        result.Status.Should().Be("Disputed");
        result.DisputeReason.Should().Be("Service was not completed as agreed.");
        result.HoldCancelled.Should().BeTrue();

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<EscrowTransaction>(), _ct), Times.Once);
        _eventBusMock.Verify(
            e => e.PublishAsync(
                It.Is<DisputeRaisedEvent>(evt =>
                    evt.TransactionId == 1 &&
                    evt.DisputeReason == "Service was not completed as agreed." &&
                    evt.RaisedBy == "client@example.com"),
                _ct),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReleasedTransaction_ThrowsInvalidOperationException()
    {
        // Arrange
        var transaction = CreateReleasedTransaction(2);
        var command = new DisputeFundsCommand(
            TransactionId: 2,
            Reason: "Service was not completed as agreed.",
            RaisedBy: "client@example.com",
            IdempotencyKey: "idempotency-key-dispute-2");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(2, _ct))
            .ReturnsAsync(transaction);

        var handler = CreateHandler();

        // Act & Assert
        await handler.Invoking(h => h.Handle(command, _ct))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot dispute transaction in status 'Completed (Released)'*");
    }
}
