using MediatR;

namespace EscrowApp.Features.Escrow.HoldFunds;

/// <summary>
/// MediatR Command for the HoldFunds slice.
/// Encapsulates all input for placing a payment hold.
/// 
/// IdempotencyKey is required for Stripe manual capture idempotency — prevents duplicate
/// charges if the caller retries the request.
/// </summary>
public sealed record HoldFundsCommand(
    int TransactionId,
    string PaymentMethodId,
    string IdempotencyKey,
    string ProviderName = "Stripe") : IRequest<HoldFundsResult>;
