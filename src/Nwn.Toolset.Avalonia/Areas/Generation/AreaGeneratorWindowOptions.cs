using Avalonia.Controls;

namespace Nwn.Toolset.Avalonia.Areas.Generation;

/// <summary>How a host's Area Generator window presents itself.</summary>
/// <param name="ApplicationName">The application name shown after the window title, or empty for none.</param>
/// <param name="Icon">The window icon, or null for the platform default.</param>
public sealed record AreaGeneratorWindowOptions(string ApplicationName, WindowIcon? Icon = null);
