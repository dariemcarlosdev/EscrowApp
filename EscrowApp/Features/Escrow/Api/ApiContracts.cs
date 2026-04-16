using System.ComponentModel.DataAnnotations;

namespace EscrowApp.Features.Escrow.Api;

/// <summary>
/// API request to create a new escrow transaction and immediately hold funds.
/// </summary>
public sealed record CreateAndHoldRequest
{
    [Required, EmailAddress]
    public required string ClientEmail { get; init; }

    [Required, EmailAddress]
    public required string ConsultantEmail { get; init; }

    [Required, Range(0.01, 1_000_000)]
    public required decimal Amount { get; init; }

    [Required, MaxLength(500)]
    public required string ServiceDescription { get; init; }

    [Required]
    public required string PaymentMethodId { get; init; }

    [MaxLength(50)]
    public string ProviderName { get; init; } = "Stripe";
}

/// <summary>
/// API request to release held funds.
/// </summary>
public sealed record ReleaseFundsApiRequest
{
    public string? IdempotencyKey { get; init; }
}

/// <summary>
/// API request to raise a dispute on a held transaction.
/// </summary>
public sealed record DisputeFundsApiRequest
{
    [Required, MaxLength(1000)]
    public required string Reason { get; init; }
}

/// <summary>
/// API request to cancel (void) a held escrow transaction.
/// Requires mutual agreement — both parties must consent.
/// </summary>
public sealed record CancelFundsApiRequest
{
    [Required, MaxLength(1000)]
    public required string Reason { get; init; }
}

/// <summary>
/// Standardized API response for escrow transactions.
/// Used by all GET and mutation endpoints.
/// </summary>
public sealed record EscrowTransactionResponse
{
    public int Id { get; init; }
    public string ClientEmail { get; init; } = string.Empty;
    public string ConsultantEmail { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string ServiceDescription { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ExternalReference { get; init; }
    public string? ExternalProvider { get; init; }
    public string? DisputeReason { get; init; }

    // Platform fee breakdown — shown to caller for transparency
    public decimal PlatformFee { get; init; }
    public decimal PlatformFeePercentage { get; init; }
    public decimal TotalCharged => Amount + PlatformFee;

    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Paginated list response wrapper.
/// </summary>
public sealed record PaginatedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
