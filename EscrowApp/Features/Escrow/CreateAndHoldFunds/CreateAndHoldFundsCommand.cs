using EscrowApp.Features.Escrow.Api;
using MediatR;

namespace EscrowApp.Features.Escrow.CreateAndHoldFunds;

/// <summary>
/// Creates a new EscrowTransaction and immediately places a payment hold.
/// Single use case for the API — avoids forcing callers to create then hold separately.
/// </summary>
public sealed record CreateAndHoldFundsCommand(
    string ClientEmail,
    string ConsultantEmail,
    decimal Amount,
    string ServiceDescription,
    string PaymentMethodId,
    string ProviderName = "Stripe") : IRequest<EscrowTransactionResponse>;
