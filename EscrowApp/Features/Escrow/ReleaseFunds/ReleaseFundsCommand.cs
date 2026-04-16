using MediatR;

namespace EscrowApp.Features.Escrow.ReleaseFunds;

/// <summary>
/// MediatR Command for the ReleaseFunds slice.
/// 
/// IdempotencyKey is required for Stripe PaymentIntent capture operation idempotency.
/// </summary>
public sealed record ReleaseFundsCommand(
    int TransactionId,
    string IdempotencyKey) : IRequest<ReleaseFundsResult>;
