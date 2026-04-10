using MediatR;

namespace EscrowApp.Features.Escrow.ReleaseFunds;

/// <summary>
/// MediatR Command for the ReleaseFunds slice.
/// </summary>
public sealed record ReleaseFundsCommand(int TransactionId) : IRequest<ReleaseFundsResult>;
