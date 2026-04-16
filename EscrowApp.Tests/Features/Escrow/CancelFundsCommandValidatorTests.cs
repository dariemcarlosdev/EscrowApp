using FluentValidation.TestHelper;
using EscrowApp.Features.Escrow.CancelFunds;

namespace EscrowApp.Tests.Features.Escrow;

public sealed class CancelFundsCommandValidatorTests
{
    private readonly CancelFundsCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_All_Fields_Valid()
    {
        // Arrange
        var command = new CancelFundsCommand(
            TransactionId: 999,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_TransactionId_Is_Zero()
    {
        // Arrange
        var command = new CancelFundsCommand(
            TransactionId: 0,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorMessage("Transaction ID must be a positive integer.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Empty()
    {
        // Arrange
        var command = new CancelFundsCommand(
            TransactionId: 999,
            Reason: "",
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Cancellation reason is required.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Too_Short()
    {
        // Arrange
        var command = new CancelFundsCommand(
            TransactionId: 999,
            Reason: "no",
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Cancellation reason must be at least 5 characters.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Exceeds_Maximum_Length()
    {
        // Arrange
        var longReason = new string('x', 501);
        var command = new CancelFundsCommand(
            TransactionId: 999,
            Reason: longReason,
            CancelledBy: "client@example.com",
            IdempotencyKey: "idempotency-key-cancel");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Cancellation reason cannot exceed 500 characters.");
    }

    [Fact]
    public void Should_Fail_When_CancelledBy_Is_Empty()
    {
        // Arrange
        var command = new CancelFundsCommand(
            TransactionId: 999,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "",
            IdempotencyKey: "idempotency-key-cancel");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CancelledBy)
            .WithErrorMessage("CancelledBy (email) is required.");
    }

    [Fact]
    public void Should_Fail_When_CancelledBy_Email_Is_Invalid()
    {
        // Arrange
        var command = new CancelFundsCommand(
            TransactionId: 999,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "invalid-email",
            IdempotencyKey: "idempotency-key-cancel");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CancelledBy)
            .WithErrorMessage("CancelledBy must be a valid email address.");
    }

    [Fact]
    public void Should_Fail_When_IdempotencyKey_Is_Empty()
    {
        // Arrange
        var command = new CancelFundsCommand(
            TransactionId: 999,
            Reason: "Mutual agreement to cancel",
            CancelledBy: "client@example.com",
            IdempotencyKey: "");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage("Idempotency key is required.");
    }
}
