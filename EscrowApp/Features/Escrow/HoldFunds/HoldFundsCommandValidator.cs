using FluentValidation;

namespace EscrowApp.Features.Escrow.HoldFunds;

public sealed class HoldFundsCommandValidator
    : AbstractValidator<HoldFundsCommand>
{
    public HoldFundsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0)
            .WithMessage("Transaction ID must be a positive integer.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required.")
            .MaximumLength(255)
            .WithMessage("Idempotency key cannot exceed 255 characters.");

        RuleFor(x => x.PaymentMethodId)
            .NotEmpty()
            .WithMessage("Payment method ID is required.");
    }
}
