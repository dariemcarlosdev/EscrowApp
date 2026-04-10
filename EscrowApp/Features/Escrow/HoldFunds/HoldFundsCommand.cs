using MediatR;

namespace EscrowApp.Features.Escrow.HoldFunds;

/// <summary>
/// MediatR Command for the HoldFunds slice.
/// Encapsulates all input for placing a payment hold.
/// </summary>
public sealed record HoldFundsCommand(
    int TransactionId,
    string PaymentMethodId,
    string ProviderName = "Stripe") : IRequest<HoldFundsResult>;
