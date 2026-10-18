namespace TaktChase.Game.Startup;

/// <summary>
/// Graphics backend requested by the user (the <c>--renderer=</c> argument).
/// This is only what was REQUESTED; the backend that was actually initialized always comes from the framework host.
/// </summary>
public enum RendererRequest
{
    /// <summary>The framework's own selection and fallback strategy.</summary>
    Auto,

    /// <summary>The framework's official GLRenderer (OpenGL).</summary>
    OpenGL,

    /// <summary>The framework's official VeldridRenderer with a Vulkan surface.</summary>
    Vulkan,
}
