using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EscrowApp.Models;

/// <summary>
/// Maps an Actor to a specific identity provider (§0.1 Hybrid Identity pillar).
/// Provider examples: "Email", "Google", "MetaMask", "WalletConnect".
/// </summary>
public class IdentityMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ActorId { get; set; }

    /// <summary>"Email", "Google", "MetaMask", "WalletConnect"</summary>
    [Required]
    public string Provider { get; set; } = string.Empty;

    /// <summary>Email address, OAuth sub claim, or wallet address.</summary>
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    [ForeignKey(nameof(ActorId))]
    public Actor Actor { get; set; } = null!;
}
