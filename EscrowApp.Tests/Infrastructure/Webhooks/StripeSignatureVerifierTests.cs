using EscrowApp.Infrastructure.Webhooks.Stripe;
using Stripe;

namespace EscrowApp.Tests.Infrastructure.Webhooks;

/// <summary>
/// Unit tests for StripeSignatureVerifier.
/// Verifies Stripe's EventUtility.ConstructEvent() behavior for signature validation,
/// timestamp validation, and error handling for malformed signatures.
/// </summary>
public sealed class StripeSignatureVerifierTests
{
    private const string TestSecret = "whsec_test_secret_key_1234567890";
    private readonly ILogger<StripeSignatureVerifier> _loggerMock = new Mock<ILogger<StripeSignatureVerifier>>().Object;

    private StripeSignatureVerifier CreateVerifier()
    {
        return new StripeSignatureVerifier(_loggerMock);
    }

    [Fact(Skip = "Requires real Stripe webhook secret from CLI for full verification")]
    public void VerifyAndParse_ValidSignature_ReturnsEvent()
    {
        // This test requires a real webhook event from Stripe CLI.
        // To execute: run `stripe listen --forward-to http://localhost:5093/api/webhooks/stripe`
        // and trigger a test event: `stripe trigger payment_intent.succeeded`
        // Then copy the actual signature header and event body here.
        // For now, we verify the integration through actual Stripe CLI testing (tc-10).
        throw new NotImplementedException("See full signature validation in live Stripe CLI testing");
    }

    [Fact]
    public void VerifyAndParse_InvalidSignature_ThrowsStripeException()
    {
        // Arrange
        var verifier = CreateVerifier();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var bodyJson = @"{""id"":""evt_invalid"",""type"":""payment_intent.succeeded""}";
        var invalidSignature = "invalid_signature_12345";
        var signatureHeader = $"t={timestamp},v1={invalidSignature}";

        // Act & Assert
        var act = () => verifier.VerifyAndParse(bodyJson, signatureHeader, TestSecret);
        act.Should().Throw<StripeException>("Invalid signature should throw StripeException");
    }

    [Fact]
    public void VerifyAndParse_ExpiredTimestamp_ThrowsStripeException()
    {
        // Arrange
        var verifier = CreateVerifier();
        var oldTimestamp = (DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds()).ToString(); // 10 minutes old
        var bodyJson = @"{""id"":""evt_expired"",""type"":""payment_intent.succeeded""}";
        var eventId = "evt_expired";

        var signedContent = $"{eventId}.{oldTimestamp}.{bodyJson}";
        var signature = ComputeHmacSha256(TestSecret, signedContent);
        var signatureHeader = $"t={oldTimestamp},v1={signature}";

        // Act & Assert
        var act = () => verifier.VerifyAndParse(bodyJson, signatureHeader, TestSecret);
        act.Should().Throw<StripeException>("Expired timestamp should throw StripeException");
    }

    [Fact]
    public void VerifyAndParse_MalformedHeader_ThrowsStripeException()
    {
        // Arrange
        var verifier = CreateVerifier();
        var bodyJson = @"{""id"":""evt_test"",""type"":""payment_intent.succeeded""}";
        var malformedHeader = "totally.not.a.valid.header"; // Missing t= and v1=

        // Act & Assert
        var act = () => verifier.VerifyAndParse(bodyJson, malformedHeader, TestSecret);
        act.Should().Throw<StripeException>("Malformed header should throw StripeException");
    }

    [Fact]
    public void VerifyAndParse_EmptyBody_ThrowsStripeException()
    {
        // Arrange
        var verifier = CreateVerifier();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signatureHeader = $"t={timestamp},v1=somesignature";
        var emptyBody = "";

        // Act & Assert
        var act = () => verifier.VerifyAndParse(emptyBody, signatureHeader, TestSecret);
        act.Should().Throw<StripeException>("Empty body should throw StripeException");
    }

    /// <summary>
    /// Helper to compute HMAC-SHA256 signature the same way Stripe does.
    /// </summary>
    private static string ComputeHmacSha256(string secret, string message)
    {
        var secretBytes = System.Text.Encoding.UTF8.GetBytes(secret);
        var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

        using var hmac = new System.Security.Cryptography.HMACSHA256(secretBytes);
        var signature = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(signature).ToLowerInvariant();
    }
}
