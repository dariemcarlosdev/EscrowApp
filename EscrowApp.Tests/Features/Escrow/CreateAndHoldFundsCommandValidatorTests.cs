using FluentValidation.TestHelper;
using EscrowApp.Features.Escrow.CreateAndHoldFunds;

namespace EscrowApp.Tests.Features.Escrow;

public sealed class CreateAndHoldFundsCommandValidatorTests
{
    private readonly CreateAndHoldFundsCommandValidator _validator = new();

    [Fact]
    public void Should_Pass_When_All_Fields_Valid()
    {
        // Arrange
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "client@example.com",
            ConsultantEmail: "consultant@example.com",
            Amount: 1000.00m,
            ServiceDescription: "Web development services",
            PaymentMethodId: "pm_test_123",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_Amount_Is_Zero()
    {
        // Arrange
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "client@example.com",
            ConsultantEmail: "consultant@example.com",
            Amount: 0m,
            ServiceDescription: "Web development services",
            PaymentMethodId: "pm_test_123",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Escrow amount must be greater than zero.");
    }

    [Fact]
    public void Should_Fail_When_Amount_Exceeds_Maximum()
    {
        // Arrange
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "client@example.com",
            ConsultantEmail: "consultant@example.com",
            Amount: 500_001m,
            ServiceDescription: "Web development services",
            PaymentMethodId: "pm_test_123",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Single transaction limit is $500,000.");
    }

    [Fact]
    public void Should_Fail_When_Client_Email_Is_Empty()
    {
        // Arrange
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "",
            ConsultantEmail: "consultant@example.com",
            Amount: 1000.00m,
            ServiceDescription: "Web development services",
            PaymentMethodId: "pm_test_123",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ClientEmail)
            .WithErrorMessage("Client email is required.");
    }

    [Fact]
    public void Should_Fail_When_Client_Email_Is_Invalid()
    {
        // Arrange
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "invalid-email",
            ConsultantEmail: "consultant@example.com",
            Amount: 1000.00m,
            ServiceDescription: "Web development services",
            PaymentMethodId: "pm_test_123",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ClientEmail)
            .WithErrorMessage("Client email must be a valid email address.");
    }

    [Fact]
    public void Should_Fail_When_Client_And_Consultant_Emails_Are_Equal()
    {
        // Arrange
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "same@example.com",
            ConsultantEmail: "same@example.com",
            Amount: 1000.00m,
            ServiceDescription: "Web development services",
            PaymentMethodId: "pm_test_123",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ConsultantEmail)
            .WithErrorMessage("Client and consultant cannot be the same person.");
    }

    [Fact]
    public void Should_Fail_When_ServiceDescription_Is_Empty()
    {
        // Arrange
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "client@example.com",
            ConsultantEmail: "consultant@example.com",
            Amount: 1000.00m,
            ServiceDescription: "",
            PaymentMethodId: "pm_test_123",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ServiceDescription)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Should_Fail_When_ServiceDescription_Exceeds_Maximum_Length()
    {
        // Arrange
        var longDescription = new string('x', 501);
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "client@example.com",
            ConsultantEmail: "consultant@example.com",
            Amount: 1000.00m,
            ServiceDescription: longDescription,
            PaymentMethodId: "pm_test_123",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ServiceDescription)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void Should_Fail_When_IdempotencyKey_Is_Empty()
    {
        // Arrange
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "client@example.com",
            ConsultantEmail: "consultant@example.com",
            Amount: 1000.00m,
            ServiceDescription: "Web development services",
            PaymentMethodId: "pm_test_123",
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
        var command = new CreateAndHoldFundsCommand(
            ClientEmail: "client@example.com",
            ConsultantEmail: "consultant@example.com",
            Amount: 1000.00m,
            ServiceDescription: "Web development services",
            PaymentMethodId: "",
            IdempotencyKey: "idempotency-key-123");

        // Act & Assert
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethodId)
            .WithErrorMessage("Payment method ID is required.");
    }
}
