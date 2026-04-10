using System.ComponentModel.DataAnnotations;

namespace EscrowApp.Models;

/// <summary>
/// Provider-agnostic user identity (§0.1 Hybrid Identity pillar).
/// Supports Web2 (Email/OAuth) and Web3 (WalletAddress) without changing this entity.
/// </summary>
public class Actor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    // Web3-ready: null until a wallet is linked
    public string? WalletAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
