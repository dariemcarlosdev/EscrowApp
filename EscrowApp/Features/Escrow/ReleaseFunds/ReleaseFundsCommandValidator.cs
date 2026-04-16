using FluentValidation;

namespace EscrowApp.Features.Escrow.ReleaseFunds;

public sealed class ReleaseFundsCommandValidator
    : AbstractValidator<ReleaseFundsCommand>
{
    public ReleaseFundsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0)
            .WithMessage("Transaction ID must be a positive integer.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required.")
            .MaximumLength(255)
            .WithMessage("Idempotency key cannot exceed 255 characters.");
    }
}
