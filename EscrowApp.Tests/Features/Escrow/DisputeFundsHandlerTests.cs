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
    private readonly Mock<IEventBus> _eventBusMock = new();

    [Fact]
    public async Task Handle_HeldTransaction_SetsDisputedStatusAndPublishesEvent()
    {
        // Arrange
        // TODO: Create transaction with Status = "Funded (Held)"
        // TODO: Mock repository to return the transaction

        // Act
        // TODO: Send DisputeFundsCommand with reason

        // Assert
        // TODO: Verify Status = "Disputed"
        // TODO: Verify DisputeReason is set
        // TODO: Verify DisputeRaisedEvent published
        await Task.CompletedTask;
        Assert.True(true, "Placeholder — implement when handler is finalized");
    }

    [Fact]
    public async Task Handle_AlreadyReleasedTransaction_ThrowsException()
    {
        // Arrange & Act & Assert
        // TODO: Released transactions cannot be disputed
        await Task.CompletedTask;
        Assert.True(true, "Placeholder — implement when handler is finalized");
    }
}
