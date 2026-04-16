using FluentValidation.TestHelper;
using EscrowApp.Features.Escrow.DisputeFunds;

namespace EscrowApp.Tests.Features.Escrow;

public sealed class DisputeFundsCommandValidatorTests
{
    private readonly DisputeFundsCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_All_Fields_Valid()
    {
        // Arrange
        var command = new DisputeFundsCommand(
            TransactionId: 789,
            Reason: "Service was not completed as agreed.",
            RaisedBy: "client@example.com",
            IdempotencyKey: "idempotency-key-dispute");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_TransactionId_Is_Zero()
    {
        // Arrange
        var command = new DisputeFundsCommand(
            TransactionId: 0,
            Reason: "Service was not completed as agreed.",
            RaisedBy: "client@example.com",
            IdempotencyKey: "idempotency-key-dispute");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorMessage("Transaction ID must be a positive integer.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Empty()
    {
        // Arrange
        var command = new DisputeFundsCommand(
            TransactionId: 789,
            Reason: "",
            RaisedBy: "client@example.com",
            IdempotencyKey: "idempotency-key-dispute");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Dispute reason is required.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Is_Too_Short()
    {
        // Arrange
        var command = new DisputeFundsCommand(
            TransactionId: 789,
            Reason: "Too short",
            RaisedBy: "client@example.com",
            IdempotencyKey: "idempotency-key-dispute");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Dispute reason must be at least 10 characters.");
    }

    [Fact]
    public void Should_Fail_When_Reason_Exceeds_Maximum_Length()
    {
        // Arrange
        var longReason = new string('x', 1001);
        var command = new DisputeFundsCommand(
            TransactionId: 789,
            Reason: longReason,
            RaisedBy: "client@example.com",
            IdempotencyKey: "idempotency-key-dispute");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Dispute reason cannot exceed 1,000 characters.");
    }

    [Fact]
    public void Should_Fail_When_RaisedBy_Is_Empty()
    {
        // Arrange
        var command = new DisputeFundsCommand(
            TransactionId: 789,
            Reason: "Service was not completed as agreed.",
            RaisedBy: "",
            IdempotencyKey: "idempotency-key-dispute");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RaisedBy)
            .WithErrorMessage("RaisedBy (email) is required.");
    }

    [Fact]
    public void Should_Fail_When_RaisedBy_Email_Is_Invalid()
    {
        // Arrange
        var command = new DisputeFundsCommand(
            TransactionId: 789,
            Reason: "Service was not completed as agreed.",
            RaisedBy: "invalid-email",
            IdempotencyKey: "idempotency-key-dispute");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RaisedBy)
            .WithErrorMessage("RaisedBy must be a valid email address.");
    }

    [Fact]
    public void Should_Fail_When_IdempotencyKey_Is_Empty()
    {
        // Arrange
        var command = new DisputeFundsCommand(
            TransactionId: 789,
            Reason: "Service was not completed as agreed.",
            RaisedBy: "client@example.com",
            IdempotencyKey: "");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IdempotencyKey)
            .WithErrorMessage("Idempotency key is required.");
    }
}
