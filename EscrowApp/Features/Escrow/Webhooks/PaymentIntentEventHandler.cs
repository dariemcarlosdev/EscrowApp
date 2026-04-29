using EscrowApp.Events;
using EscrowApp.Infrastructure.Webhooks.Stripe;
using EscrowApp.Models.Repositories;
using MediatR;

namespace EscrowApp.Features.Escrow.Webhooks;

/// <summary>
/// MediatR notification handler for payment_intent.succeeded events from Stripe.
/// Triggered by StripeWebhookEndpoint after signature verification.
///
/// Responsibilities:
/// - Correlate Stripe PaymentIntent ID to EscrowTransaction via ExternalReference
/// - Verify transaction exists and is in a valid state
/// - Log confirmation of successful hold
/// - Publish PaymentReceivedEvent for downstream workflow triggers
/// - Never fails (webhook endpoint must return 200 OK to Stripe)
///
/// This is OBSERVATIONAL LOGIC only — webhook confirms holds that already happened
/// synchronously via HoldFundsCommand. Status remains "Held" (MVP behavior).
/// </summary>
public sealed class PaymentIntentEventHandler(
    IEscrowTransactionRepository transactionRepository,
    IEventBus eventBus,
    ILogger<PaymentIntentEventHandler> logger) 
    : INotificationHandler<PaymentIntentSucceededNotification>
{
    /// <summary>
    /// Handles payment_intent.succeeded — confirms Stripe hold is active.
    /// Updates transaction with verification timestamp and publishes domain event.
    /// </summary>
    public async Task Handle(
        PaymentIntentSucceededNotification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "🔔 Processing payment_intent.succeeded: EventId={EventId}, PaymentIntentId={PaymentIntentId}, Amount={Amount}{Currency}",
                notification.StripeEventId,
                notification.PaymentIntentId,
                notification.Amount / 100m,
                notification.Currency.ToUpper());

            // Find transaction by Stripe PaymentIntent ID (stored in ExternalReference)
            var transaction = await transactionRepository.GetByExternalReferenceAsync(
                notification.PaymentIntentId,
                cancellationToken);

            if (transaction is null)
            {
                // Transaction not found — log but do NOT throw (webhook must succeed)
                logger.LogWarning(
                    "⚠️ Webhook received for unknown PaymentIntent: {PaymentIntentId} — ignoring",
                    notification.PaymentIntentId);
                return;
            }

            // Verify transaction is in a state that expects a hold confirmation
            if (transaction.Status != "Held" && transaction.Status != "Pending")
            {
                logger.LogWarning(
                    "⚠️ PaymentIntent confirmed but transaction in unexpected status: {TransactionId}, Status={Status} — ignoring",
                    transaction.Id,
                    transaction.Status);
                return;
            }

            // Verify amount matches (prevents tampering or API errors)
            var expectedAmount = (long)(transaction.Amount * 100); // Convert to cents
            if (notification.Amount != expectedAmount)
            {
                logger.LogError(
                    "❌ Amount mismatch on PaymentIntent {PaymentIntentId}: expected {Expected}, got {Actual}",
                    notification.PaymentIntentId,
                    expectedAmount,
                    notification.Amount);
                return;
            }

            // Verify external provider is set to "Stripe"
            if (transaction.ExternalProvider != "Stripe")
            {
                logger.LogWarning(
                    "⚠️ PaymentIntent confirmed for transaction with unexpected provider: {TransactionId}, Provider={Provider}",
                    transaction.Id,
                    transaction.ExternalProvider);
                return;
            }

            // Transaction is valid — publish domain event for downstream listeners
            // (Email confirmation, dashboard update, future payment release automation)
            var paymentEvent = new PaymentReceivedEvent
            {
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                ExternalReference = transaction.ExternalReference ?? string.Empty,
                Provider = transaction.ExternalProvider ?? "Stripe",
                PlatformFee = transaction.PlatformFee,
                PlatformFeePercentage = transaction.PlatformFeePercentage
            };

            await eventBus.PublishAsync(paymentEvent, cancellationToken);

            logger.LogInformation(
                "✅ Payment confirmed and event published: TransactionId={TransactionId}, PaymentIntentId={PaymentIntentId}",
                transaction.Id,
                notification.PaymentIntentId);
        }
        catch (Exception ex)
        {
            // Log unexpected errors, but DO NOT throw to webhook endpoint
            // (Throwing would cause StripeWebhookEndpoint to return 500, triggering Stripe retries)
            logger.LogError(
                ex,
                "❌ Unexpected error processing payment_intent.succeeded webhook: {PaymentIntentId}",
                notification.PaymentIntentId);
        }
    }
}
