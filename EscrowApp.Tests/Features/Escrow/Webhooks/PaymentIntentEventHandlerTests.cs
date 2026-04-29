using EscrowApp.Events;
using EscrowApp.Features.Escrow.Webhooks;
using EscrowApp.Infrastructure.Webhooks.Stripe;
using EscrowApp.Models;
using EscrowApp.Models.Repositories;

namespace EscrowApp.Tests.Features.Escrow.Webhooks;

/// <summary>
/// Unit tests for the PaymentIntentEventHandler MediatR notification handler.
/// Verifies webhook event processing: transaction lookup, state validation,
/// amount verification, and domain event publishing.
/// </summary>
public sealed class PaymentIntentEventHandlerTests
{
    private readonly Mock<IEscrowTransactionRepository> _repositoryMock = new();
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly Mock<ILogger<PaymentIntentEventHandler>> _loggerMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private PaymentIntentEventHandler CreateHandler()
    {
        return new PaymentIntentEventHandler(_repositoryMock.Object, _eventBusMock.Object, _loggerMock.Object);
    }

    private EscrowTransaction CreateHeldTransaction(int id = 1, decimal amount = 1000.00m, string externalRef = "pi_test_12345")
    {
        return new EscrowTransaction
        {
            Id = id,
            ClientEmail = "client@example.com",
            ConsultantEmail = "consultant@example.com",
            Amount = amount,
            ServiceDescription = "Web development",
            Status = "Held",
            ExternalReference = externalRef,
            ExternalProvider = "Stripe",
            CreatedAt = DateTime.UtcNow
        };
    }

    private PaymentIntentSucceededNotification CreateNotification(
        string paymentIntentId = "pi_test_12345",
        long amount = 100000, // 1000.00 in cents
        string currency = "usd")
    {
        return new PaymentIntentSucceededNotification(
            PaymentIntentId: paymentIntentId,
            Amount: amount,
            Currency: currency,
            StripeEventId: "evt_test_event_id");
    }

    /// <summary>
    /// Test: Transaction found in "Held" state → publishes PaymentReceivedEvent
    /// </summary>
    [Fact]
    public async Task Handle_HeldTransactionFound_PublishesPaymentReceivedEvent()
    {
        // Arrange
        var transaction = CreateHeldTransaction(id: 1, amount: 1000.00m, externalRef: "pi_test_12345");
        var notification = CreateNotification(paymentIntentId: "pi_test_12345", amount: 100000);

        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync("pi_test_12345", _ct))
            .ReturnsAsync(transaction);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), _ct))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.Handle(notification, _ct);

        // Assert
        _repositoryMock.Verify(
            r => r.GetByExternalReferenceAsync("pi_test_12345", _ct),
            Times.Once,
            "Repository should look up transaction by PaymentIntent ID");

        _eventBusMock.Verify(
            e => e.PublishAsync(
                It.Is<PaymentReceivedEvent>(
                    ev => ev.TransactionId == 1 &&
                          ev.ExternalReference == "pi_test_12345" &&
                          ev.Provider == "Stripe"),
                _ct),
            Times.Once,
            "Event bus should publish PaymentReceivedEvent with correct details");
    }

    /// <summary>
    /// Test: Transaction not found by PaymentIntent ID → logs warning, does not throw
    /// </summary>
    [Fact]
    public async Task Handle_TransactionNotFound_LogsWarningAndReturns()
    {
        // Arrange
        var notification = CreateNotification(paymentIntentId: "pi_unknown_12345");

        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync("pi_unknown_12345", _ct))
            .ReturnsAsync((EscrowTransaction?)null);

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(notification, _ct);

        // Assert
        await act.Should().NotThrowAsync("Handler should not throw on unknown transaction");

        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Event should not be published for unknown transaction");
    }

    /// <summary>
    /// Test: Transaction in "Pending" state also triggers event publishing
    /// </summary>
    [Fact]
    public async Task Handle_PendingTransactionFound_PublishesEvent()
    {
        // Arrange
        var transaction = new EscrowTransaction
        {
            Id = 2,
            ClientEmail = "client@example.com",
            ConsultantEmail = "consultant@example.com",
            Amount = 500.00m,
            ServiceDescription = "Consulting",
            Status = "Pending",
            ExternalReference = "pi_pending_999",
            ExternalProvider = "Stripe",
            CreatedAt = DateTime.UtcNow
        };

        var notification = CreateNotification(paymentIntentId: "pi_pending_999", amount: 50000);

        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync("pi_pending_999", _ct))
            .ReturnsAsync(transaction);

        _eventBusMock
            .Setup(e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), _ct))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        await handler.Handle(notification, _ct);

        // Assert
        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), _ct),
            Times.Once,
            "Event should publish for Pending status");
    }

    /// <summary>
    /// Test: Amount mismatch between webhook and transaction → logs error, does not publish
    /// </summary>
    [Fact]
    public async Task Handle_AmountMismatch_LogsErrorAndDoesNotPublish()
    {
        // Arrange
        var transaction = CreateHeldTransaction(id: 3, amount: 1000.00m, externalRef: "pi_mismatch_888");
        var notification = CreateNotification(paymentIntentId: "pi_mismatch_888", amount: 50000); // 500.00, not 1000.00

        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync("pi_mismatch_888", _ct))
            .ReturnsAsync(transaction);

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(notification, _ct);

        // Assert
        await act.Should().NotThrowAsync("Handler should not throw on amount mismatch");

        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Event should not publish on amount mismatch (prevents tampering)");
    }

    /// <summary>
    /// Test: External provider mismatch (expected "Stripe", got something else) → logs warning, no publish
    /// </summary>
    [Fact]
    public async Task Handle_ProviderMismatch_LogsWarningAndDoesNotPublish()
    {
        // Arrange
        var transaction = new EscrowTransaction
        {
            Id = 4,
            ClientEmail = "client@example.com",
            ConsultantEmail = "consultant@example.com",
            Amount = 1000.00m,
            ServiceDescription = "Web development",
            Status = "Held",
            ExternalReference = "pi_test_12345",
            ExternalProvider = "PayPal", // Provider mismatch
            CreatedAt = DateTime.UtcNow
        };

        var notification = CreateNotification(paymentIntentId: "pi_test_12345", amount: 100000);

        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync("pi_test_12345", _ct))
            .ReturnsAsync(transaction);

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(notification, _ct);

        // Assert
        await act.Should().NotThrowAsync("Handler should not throw on provider mismatch");

        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Event should not publish if provider doesn't match");
    }

    /// <summary>
    /// Test: Unexpected exception during processing → logs error, does not throw
    /// (Webhook handler must always return success to Stripe)
    /// </summary>
    [Fact]
    public async Task Handle_RepositoryThrowsException_LogsErrorAndDoesNotThrow()
    {
        // Arrange
        var notification = CreateNotification();
        var exception = new InvalidOperationException("Database connection failed");

        _repositoryMock
            .Setup(r => r.GetByExternalReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var handler = CreateHandler();

        // Act
        var act = async () => await handler.Handle(notification, _ct);

        // Assert
        await act.Should().NotThrowAsync("Handler should catch and log exceptions, not throw");

        _eventBusMock.Verify(
            e => e.PublishAsync(It.IsAny<PaymentReceivedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Event should not publish if repository fails");
    }
}
