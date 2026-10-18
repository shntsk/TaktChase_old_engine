namespace TaktChase.Game.Graphics;

/// <summary>
/// Plain values read from the initialized framework host. Keeping them in a separate type lets
/// <see cref="GraphicsDiagnostics.Build"/> be tested without a window, a renderer, or a GPU.
/// Every property is either a value the host actually reported or <c>null</c> when the host did not report it.
/// </summary>
public sealed class GraphicsSnapshot
{
    /// <summary>Text from <c>GameHost.RendererInfo</c>, or <c>null</c> if the host reported none.</summary>
    public string? RendererInfo { get; init; }

    /// <summary>Type name of the active <c>GameHost.Renderer</c> (for example <c>GLRenderer</c>), or <c>null</c> if there is no renderer.</summary>
    public string? RendererTypeName { get; init; }

    /// <summary>Whether <c>GameHost.Window</c> exists. It does not exist on headless hosts.</summary>
    public bool HasWindow { get; init; }

    /// <summary>
    /// <c>GameHost.ResolvedRenderer</c> as text. Only meaningful for the framework's automatic selection;
    /// <c>null</c> when the renderer was selected through <c>OSU_GRAPHICS_*</c> environment variables.
    /// </summary>
    public string? ResolvedRenderer { get; init; }

    /// <summary>True when <c>OSU_GRAPHICS_RENDERER</c> or <c>OSU_GRAPHICS_SURFACE</c> was set for this process.</summary>
    public bool SelectedViaEnvironment { get; init; }

    public bool IsWindows { get; init; }
}
