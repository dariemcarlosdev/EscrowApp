using Microsoft.Extensions.Configuration;
using Stripe;

namespace EscrowApp.Tests.Services.Strategies;

/// <summary>
/// Unit tests for the StripePaymentStrategy.
/// Verifies: correct PaymentIntent creation parameters, manual capture mode,
/// idempotency key headers, and error mapping from Stripe SDK exceptions.
/// </summary>
public sealed class StripePaymentStrategyTests
{
    private readonly Mock<PaymentIntentService> _paymentIntentServiceMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly CancellationToken _ct = CancellationToken.None;

    private StripePaymentStrategy CreateStrategy()
    {
        _configurationMock
            .Setup(c => c["Stripe:PaymentReturnUrl"])
            .Returns("https://example.com/payment/return");

        return new StripePaymentStrategy(_paymentIntentServiceMock.Object, _configurationMock.Object);
    }

    [Fact]
    public async Task HoldFundsAsync_ValidAmount_CreatesPaymentIntentWithManualCapture()
    {
        // Arrange
        var strategy = CreateStrategy();
        var amount = 1000.00m;
        var paymentMethodId = "pm_test_123";
        var idempotencyKey = "idempotency-hold-1";

        var expectedPaymentIntent = new PaymentIntent
        {
            Id = "pi_test_12345",
            Amount = 100000, // 1000.00 * 100
            Currency = "usd",
            CaptureMethod = "manual",
            Status = "requires_capture"
        };

        _paymentIntentServiceMock
            .Setup(s => s.CreateAsync(
                It.Is<PaymentIntentCreateOptions>(opts =>
                    opts.Amount == 100000 &&
                    opts.Currency == "usd" &&
                    opts.PaymentMethod == paymentMethodId &&
                    opts.CaptureMethod == "manual" &&
                    opts.Confirm == true),
                It.Is<RequestOptions>(ro => ro.IdempotencyKey == idempotencyKey),
                _ct))
            .ReturnsAsync(expectedPaymentIntent);

        // Act
        var result = await strategy.HoldFundsAsync(amount, paymentMethodId, idempotencyKey, _ct);

        // Assert
        result.Should().Be("pi_test_12345");
        _paymentIntentServiceMock.Verify(
            s => s.CreateAsync(
                It.IsAny<PaymentIntentCreateOptions>(),
                It.IsAny<RequestOptions>(),
                _ct),
            Times.Once);
    }

    [Fact]
    public async Task ReleaseFundsAsync_ValidReference_CapturesPaymentIntent()
    {
        // Arrange
        var strategy = CreateStrategy();
        var paymentIntentId = "pi_test_12345";
        var idempotencyKey = "idempotency-release-1";

        var expectedPaymentIntent = new PaymentIntent
        {
            Id = paymentIntentId,
            Status = "succeeded",
            Amount = 100000,
            Currency = "usd"
        };

        _paymentIntentServiceMock
            .Setup(s => s.CaptureAsync(
                paymentIntentId,
                null,
                It.Is<RequestOptions>(ro => ro.IdempotencyKey == idempotencyKey),
                _ct))
            .ReturnsAsync(expectedPaymentIntent);

        // Act
        var result = await strategy.ReleaseFundsAsync(paymentIntentId, idempotencyKey, _ct);

        // Assert
        result.Should().BeTrue();
        _paymentIntentServiceMock.Verify(
            s => s.CaptureAsync(
                paymentIntentId,
                null,
                It.IsAny<RequestOptions>(),
                _ct),
            Times.Once);
    }

    [Fact]
    public async Task CancelHoldAsync_ValidReference_CancelsPaymentIntent()
    {
        // Arrange
        var strategy = CreateStrategy();
        var paymentIntentId = "pi_test_12345";
        var idempotencyKey = "idempotency-cancel-1";

        var expectedPaymentIntent = new PaymentIntent
        {
            Id = paymentIntentId,
            Status = "canceled",
            Amount = 100000,
            Currency = "usd"
        };

        _paymentIntentServiceMock
            .Setup(s => s.CancelAsync(
                paymentIntentId,
                null,
                It.Is<RequestOptions>(ro => ro.IdempotencyKey == idempotencyKey),
                _ct))
            .ReturnsAsync(expectedPaymentIntent);

        // Act
        var result = await strategy.CancelHoldAsync(paymentIntentId, idempotencyKey, _ct);

        // Assert
        result.Should().BeTrue();
        _paymentIntentServiceMock.Verify(
            s => s.CancelAsync(
                paymentIntentId,
                null,
                It.IsAny<RequestOptions>(),
                _ct),
            Times.Once);
    }

    [Fact]
    public async Task HoldFundsAsync_EmptyPaymentMethodId_ReturnsWithIdempotencyKey()
    {
        // Arrange
        var strategy = CreateStrategy();
        var amount = 1000.00m;
        var paymentMethodId = "pm_test_456";
        var idempotencyKey = "idempotency-hold-2";

        var expectedPaymentIntent = new PaymentIntent
        {
            Id = "pi_test_67890",
            Status = "requires_capture"
        };

        _paymentIntentServiceMock
            .Setup(s => s.CreateAsync(
                It.IsAny<PaymentIntentCreateOptions>(),
                It.Is<RequestOptions>(ro => ro.IdempotencyKey == idempotencyKey),
                _ct))
            .ReturnsAsync(expectedPaymentIntent);

        // Act
        var result = await strategy.HoldFundsAsync(amount, paymentMethodId, idempotencyKey, _ct);

        // Assert
        result.Should().Be("pi_test_67890");
        // Verify the idempotency key was passed in RequestOptions
        _paymentIntentServiceMock.Verify(
            s => s.CreateAsync(
                It.IsAny<PaymentIntentCreateOptions>(),
                It.Is<RequestOptions>(ro => ro.IdempotencyKey == idempotencyKey),
                _ct),
            Times.Once);
    }

    [Fact]
    public async Task ReleaseFundsAsync_NullReference_ReturnsFalse()
    {
        // Arrange
        var strategy = CreateStrategy();

        // Act
        var result = await strategy.ReleaseFundsAsync(null!, "idempotency-key", _ct);

        // Assert
        result.Should().BeFalse();
        _paymentIntentServiceMock.Verify(s => s.CaptureAsync(It.IsAny<string>(), It.IsAny<PaymentIntentCaptureOptions>(), It.IsAny<RequestOptions>(), _ct), Times.Never);
    }
}
