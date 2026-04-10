using EscrowApp.Events;
using EscrowApp.Models.Repositories;

namespace EscrowApp.Features.Escrow.Webhooks;

/// <summary>
/// Handles verified Stripe payment_intent events and correlates them
/// to domain transactions. Called from the webhook endpoint after
/// signature verification and deduplication.
///
/// Responsibilities:
/// - Map Stripe event types to domain state transitions
/// - Update EscrowTransaction status via repository
/// - Publish domain events via IEventBus
///
/// This class handles BUSINESS LOGIC only — transport/verification
/// lives in Infrastructure/Webhooks/Stripe/.
/// </summary>
internal sealed class PaymentIntentEventHandler(
    IEscrowTransactionRepository repo,
    IEventBus eventBus)
{
    // TODO: Implement handlers for each Stripe event type:

    /// <summary>
    /// Handles payment_intent.succeeded — confirms funds are captured.
    /// </summary>
    public async Task HandlePaymentSucceededAsync(string paymentIntentId, CancellationToken ct)
    {
        // TODO:
        // 1. Find transaction by ExternalReference == paymentIntentId
        // 2. Verify current status allows this transition
        // 3. Update status to "Released" (funds captured)
        // 4. Publish PaymentReceivedEvent via IEventBus
        throw new NotImplementedException();
    }

    /// <summary>
    /// Handles payment_intent.canceled — confirms hold was voided.
    /// </summary>
    public async Task HandlePaymentCanceledAsync(string paymentIntentId, CancellationToken ct)
    {
        // TODO:
        // 1. Find transaction by ExternalReference == paymentIntentId
        // 2. Update status to "Cancelled"
        // 3. Publish FundsCancelledEvent via IEventBus
        throw new NotImplementedException();
    }

    /// <summary>
    /// Handles charge.dispute.created — flags transaction as disputed externally.
    /// </summary>
    public async Task HandleDisputeCreatedAsync(string paymentIntentId, string reason, CancellationToken ct)
    {
        // TODO:
        // 1. Find transaction by ExternalReference == paymentIntentId
        // 2. Update status to "Disputed" with reason
        // 3. Publish DisputeRaisedEvent via IEventBus
        throw new NotImplementedException();
    }
}
