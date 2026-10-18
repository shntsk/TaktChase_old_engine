using System;
using System.Collections.Generic;

namespace TaktChase.Game.Startup;

/// <summary>
/// Traduz <see cref="RendererRequest"/> nas variáveis de ambiente OFICIAIS do osu-framework
/// (wiki "Environment variables"): <c>OSU_GRAPHICS_RENDERER</c> e <c>OSU_GRAPHICS_SURFACE</c>.
/// </summary>
/// <remarks>
/// O framework lê essas variáveis no construtor estático de <c>FrameworkEnvironment</c>,
/// portanto elas precisam ser definidas ANTES de qualquer chamada ao framework (antes de criar o host).
/// Quando qualquer uma delas está definida, <c>GameHost.ChooseAndSetupRenderer</c> usa exatamente
/// o par renderer/superfície pedido e falha de forma definitiva, sem fallback interno.
/// Se só a superfície for definida, o renderer assumido é "veldrid"; se só o renderer for definido,
/// a superfície assumida é OpenGL. Por isso o TaktChase sempre define as DUAS variáveis.
/// Valor de superfície inválido é ignorado em silêncio pelo framework (Enum.TryParse), e valor de renderer
/// desconhecido cai no GLRenderer.
/// </remarks>
public static class RendererEnvironment
{
    public const string RENDERER_VARIABLE = "OSU_GRAPHICS_RENDERER";
    public const string SURFACE_VARIABLE = "OSU_GRAPHICS_SURFACE";

    /// <summary>
    /// Variáveis a definir para o pedido. Para <see cref="RendererRequest.Auto"/> não se define nada,
    /// para que o framework execute a própria seleção e o próprio fallback.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetVariables(RendererRequest request)
    {
        switch (request)
        {
            case RendererRequest.OpenGL:
                // GLRenderer (renderer OpenGL "legado" do framework); só suporta a superfície opengl.
                // "gl" é o valor com ramo explícito em GameHost.SetupRendererAndWindow(string, GraphicsSurfaceType);
                // "opengl" também é documentado na wiki, mas só funcionaria por cair no ramo padrão do switch.
                return new Dictionary<string, string>
                {
                    [RENDERER_VARIABLE] = "gl",
                    [SURFACE_VARIABLE] = "opengl",
                };

            case RendererRequest.Vulkan:
                // VeldridRenderer + superfície Vulkan.
                return new Dictionary<string, string>
                {
                    [RENDERER_VARIABLE] = "veldrid",
                    [SURFACE_VARIABLE] = "vulkan",
                };

            default:
                return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Verdadeiro se o ambiente já traz uma escolha de renderer/superfície feita fora do TaktChase.
    /// Nesse caso o framework a obedece (sem fallback), mesmo com <c>--renderer=auto</c>.
    /// </summary>
    public static bool HasExternalOverride(Func<string, string?> getEnvironmentVariable)
        => !string.IsNullOrEmpty(getEnvironmentVariable(RENDERER_VARIABLE))
           || !string.IsNullOrEmpty(getEnvironmentVariable(SURFACE_VARIABLE));
}
