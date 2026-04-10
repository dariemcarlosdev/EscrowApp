using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EscrowApp.Infrastructure.Auth;

/// <summary>
/// Authentication handler that validates API keys from the X-Api-Key header.
/// Keys are stored in configuration for MVP — move to database/KeyVault for production.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var apiKeyHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var providedKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
            return Task.FromResult(AuthenticateResult.Fail("API key is empty."));

        var configuredKeys = configuration
            .GetSection("ApiKeys")
            .Get<Dictionary<string, ApiKeyConfig>>();

        if (configuredKeys is null || configuredKeys.Count == 0)
            return Task.FromResult(AuthenticateResult.Fail("No API keys configured."));

        var matchedClient = configuredKeys
            .FirstOrDefault(kvp =>
                string.Equals(kvp.Value.Key, providedKey, StringComparison.Ordinal));

        if (matchedClient.Value is null)
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, matchedClient.Key),
            new Claim(ClaimTypes.Name, matchedClient.Value.DisplayName ?? matchedClient.Key),
            new Claim("api_client_id", matchedClient.Key),
            new Claim("scope", "escrow:read escrow:write")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Configuration model for an API key entry.
/// </summary>
public sealed class ApiKeyConfig
{
    public required string Key { get; init; }
    public string? DisplayName { get; init; }
}
