namespace EscrowApp;

/// <summary>
/// Marker class for shared localization resources used across the entire application.
/// Maps to Resources/SharedResource.resx (en) and Resources/SharedResource.{culture}.resx.
/// Inject via <c>IStringLocalizer&lt;SharedResource&gt;</c> in any component or service.
/// </summary>
public sealed class SharedResource;
