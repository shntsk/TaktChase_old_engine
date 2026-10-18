namespace TaktChase.Game.Startup;

/// <summary>
/// Backend gráfico pedido pelo usuário (argumento <c>--renderer=</c>).
/// É apenas o que foi PEDIDO; o backend efetivamente inicializado vem sempre do host do framework.
/// </summary>
public enum RendererRequest
{
    /// <summary>Estratégia de seleção e fallback do próprio framework.</summary>
    Auto,

    /// <summary>GLRenderer (OpenGL) oficial do framework.</summary>
    OpenGL,

    /// <summary>VeldridRenderer com superfície Vulkan oficial do framework.</summary>
    Vulkan,
}
