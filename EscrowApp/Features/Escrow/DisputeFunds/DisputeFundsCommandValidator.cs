using FluentValidation;

namespace EscrowApp.Features.Escrow.DisputeFunds;

public sealed class DisputeFundsCommandValidator
    : AbstractValidator<DisputeFundsCommand>
{
    public DisputeFundsCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .GreaterThan(0)
            .WithMessage("Transaction ID must be a positive integer.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Dispute reason is required.")
            .MinimumLength(10)
            .WithMessage("Dispute reason must be at least 10 characters.")
            .MaximumLength(1000)
            .WithMessage("Dispute reason cannot exceed 1,000 characters.");

        RuleFor(x => x.RaisedBy)
            .NotEmpty()
            .WithMessage("RaisedBy (email) is required.")
            .EmailAddress()
            .WithMessage("RaisedBy must be a valid email address.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("Idempotency key is required.")
            .MaximumLength(255)
            .WithMessage("Idempotency key cannot exceed 255 characters.");
    }
}
