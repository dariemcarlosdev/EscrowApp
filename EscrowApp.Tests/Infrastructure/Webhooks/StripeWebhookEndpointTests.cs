using EscrowApp.Infrastructure.Webhooks.Stripe;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EscrowApp.Tests.Infrastructure.Webhooks;

public sealed class StripeWebhookEndpointTests
{
    [Fact]
    public void HandleStatus_ManualProbe_ReturnsOkWithPostGuidance()
    {
        // Arrange
        var result = StripeWebhookEndpoint.HandleStatus();

        // Act
        var okResult = result.Should().BeOfType<Ok<StripeWebhookStatusResponse>>().Subject;
        var payload = okResult.Value;

        // Assert
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        payload.Should().NotBeNull();
        payload!.Endpoint.Should().Be("/api/webhooks/stripe");
        payload.AcceptedMethod.Should().Be("POST");
        payload.Status.Should().Be("ready");
        payload.Message.Should().Contain("POST only");
        payload.Message.Should().Contain("signed POST request");
    }
}
