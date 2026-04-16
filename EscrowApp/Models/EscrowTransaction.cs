namespace EscrowApp.Models;

public class EscrowTransaction
{
    public int Id { get; set; }

    public string ClientEmail { get; set; } = string.Empty;

    public string ConsultantEmail { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string ServiceDescription { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    // §0.3 Agnostic Persistence: stores Stripe PaymentIntent IDs (Web2) or ETH tx hashes (Web3)
    public string? ExternalReference { get; set; }

    // Identifies the payment provider: "Stripe", "PayPal", "Ethereum", etc.
    public string? ExternalProvider { get; set; }

    // Set when Status = "Disputed" — records the reason raised by client or consultant
    public string? DisputeReason { get; set; }

    // Platform fee (1.5%) — calculated at creation time; immutable for audit trail integrity.
    // Stored separately so future rate changes do not retroactively alter historical records.
    public decimal PlatformFee { get; set; }

    // Rate applied at creation — snapshot for regulatory traceability (e.g., 0.015 = 1.5%)
    public decimal PlatformFeePercentage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
