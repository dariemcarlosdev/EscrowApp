using EscrowApp.Features.Escrow.Api;
using MediatR;

namespace EscrowApp.Features.Escrow.CreateAndHoldFunds;

/// <summary>
/// Creates a new EscrowTransaction and immediately places a payment hold.
/// Single use case for the API — avoids forcing callers to create then hold separately.
/// 
/// IdempotencyKey is required for Stripe manual capture idempotency — prevents duplicate
/// charges if the caller retries the request.
/// </summary>
public sealed record CreateAndHoldFundsCommand(
    string ClientEmail,
    string ConsultantEmail,
    decimal Amount,
    string ServiceDescription,
    string PaymentMethodId,
    string IdempotencyKey,
    string ProviderName = "Stripe") : IRequest<EscrowTransactionResponse>;
