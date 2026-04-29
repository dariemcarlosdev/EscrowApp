namespace EscrowApp.Models;

/// <summary>
/// Application role name constants.
///
/// Using string constants instead of an enum keeps Identity's string-based role
/// API clean and avoids `.ToString()` calls throughout the codebase.
/// </summary>
public static class AppRoles
{
    public const string Client = "Client";
    public const string Consultant = "Consultant";

    /// <summary>All roles — used for seeding and validation.</summary>
    public static readonly IReadOnlyList<string> All = [Client, Consultant];
}
