using Microsoft.AspNetCore.Identity;

namespace EscrowApp.Models;

/// <summary>
/// Application user for Blazor Server authentication.
/// Extends IdentityUser&lt;int&gt; to maintain integration with Actor (hybrid identity bridge).
/// </summary>
public sealed class ApplicationUser : IdentityUser<int>
{
    /// <summary>
    /// Foreign key to Actor — links this application user to the domain Actor entity.
    /// Optional to support Web2 → Web3 bridge (user can register without Actor, then link later).
    /// </summary>
    public int? ActorId { get; set; }

    /// <summary>
    /// Navigation property to Actor.
    /// </summary>
    public Actor? Actor { get; set; }
}
