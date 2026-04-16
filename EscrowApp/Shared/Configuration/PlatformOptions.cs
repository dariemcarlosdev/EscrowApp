namespace EscrowApp.Shared.Configuration;

/// <summary>
/// Typed configuration for NexTruzt.io platform fees.
/// Bound from the "Platform" section in appsettings.json.
///
/// Placed in Shared/Configuration/ (cross-cutting) so Application-layer handlers
/// can reference it without violating the Clean Architecture dependency rule:
/// Application → Domain only; never Application → Infrastructure.
///
/// Rate changes here affect all new transactions — existing records retain their
/// PlatformFeePercentage snapshot for audit trail integrity.
/// </summary>
public sealed record PlatformOptions
{
    public const string SectionName = "Platform";

    /// <summary>Platform fee rate (e.g., 0.015 = 1.5%).</summary>
    public decimal FeePercentage { get; init; } = 0.015m;

    /// <summary>Minimum platform fee in dollars. Prevents sub-cent edge cases on micro-transactions below $33.</summary>
    public decimal MinimumFee { get; init; } = 0.50m;

    /// <summary>Currency code for fee display and reporting (ISO 4217).</summary>
    public string Currency { get; init; } = "USD";
}
