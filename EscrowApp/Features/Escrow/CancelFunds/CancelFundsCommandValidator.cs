using FluentValidation;

namespace EscrowApp.Features.Escrow.CancelFunds;

public sealed class CancelFundsCommandValidator
    : AbstractValidator<CancelFundsCommand>
{
    public CancelFundsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0)
            .WithMessage("Transaction ID must be a positive integer.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required.")
            .MinimumLength(5)
            .WithMessage("Cancellation reason must be at least 5 characters.")
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.");

        RuleFor(x => x.CancelledBy)
            .NotEmpty()
            .WithMessage("CancelledBy (email) is required.")
            .EmailAddress()
            .WithMessage("CancelledBy must be a valid email address.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required.")
            .MaximumLength(255)
            .WithMessage("Idempotency key cannot exceed 255 characters.");
    }
}
