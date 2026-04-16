using FluentValidation;

namespace EscrowApp.Features.Escrow.CreateAndHoldFunds;

public sealed class CreateAndHoldFundsCommandValidator
    : AbstractValidator<CreateAndHoldFundsCommand>
{
    public CreateAndHoldFundsCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Escrow amount must be greater than zero.")
            .LessThanOrEqualTo(500_000)
            .WithMessage("Single transaction limit is $500,000.");

        RuleFor(x => x.ClientEmail)
            .NotEmpty()
            .WithMessage("Client email is required.")
            .EmailAddress()
            .WithMessage("Client email must be a valid email address.");

        RuleFor(x => x.ConsultantEmail)
            .NotEmpty()
            .WithMessage("Consultant email is required.")
            .EmailAddress()
            .WithMessage("Consultant email must be a valid email address.")
            .NotEqual(x => x.ClientEmail)
            .WithMessage("Client and consultant cannot be the same person.");

        RuleFor(x => x.ServiceDescription)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");

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
