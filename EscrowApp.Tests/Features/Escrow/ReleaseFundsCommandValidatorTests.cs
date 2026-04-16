using FluentValidation.TestHelper;
using EscrowApp.Features.Escrow.HoldFunds;

namespace EscrowApp.Tests.Features.Escrow;

public sealed class HoldFundsCommandValidatorTests
{
    private readonly HoldFundsCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_All_Fields_Valid()
    {
        // Arrange
        var command = new HoldFundsCommand(
            TransactionId: 123,
            PaymentMethodId: "pm_test_456",
            IdempotencyKey: "idempotency-key-456");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_TransactionId_Is_Zero()
    {
        // Arrange
        var command = new HoldFundsCommand(
            TransactionId: 0,
            PaymentMethodId: "pm_test_456",
            IdempotencyKey: "idempotency-key-456");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorMessage("Transaction ID must be a positive integer.");
    }

    [Fact]
    public void Should_Fail_When_TransactionId_Is_Negative()
    {
        // Arrange
        var command = new HoldFundsCommand(
            TransactionId: -1,
            PaymentMethodId: "pm_test_456",
            IdempotencyKey: "idempotency-key-456");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorMessage("Transaction ID must be a positive integer.");
    }

    [Fact]
    public void Should_Fail_When_IdempotencyKey_Is_Empty()
    {
        // Arrange
        var command = new HoldFundsCommand(
            TransactionId: 123,
            PaymentMethodId: "pm_test_456",
            IdempotencyKey: "");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage("Idempotency key is required.");
    }

    [Fact]
    public void Should_Fail_When_PaymentMethodId_Is_Empty()
    {
        // Arrange
        var command = new HoldFundsCommand(
            TransactionId: 123,
            PaymentMethodId: "",
            IdempotencyKey: "idempotency-key-456");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethodId)
            .WithErrorMessage("Payment method ID is required.");
    }
}
