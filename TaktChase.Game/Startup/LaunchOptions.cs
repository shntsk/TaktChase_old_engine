using System;
using System.Collections.Generic;

namespace TaktChase.Game.Startup;

/// <summary>
/// Opções de linha de comando do TaktChase. Não depende do framework, para poder ser
/// interpretada ANTES de qualquer inicialização gráfica e testada sem GPU.
/// </summary>
public sealed class LaunchOptions
{
    /// <summary>Definida pelo processo que reinicia o jogo após falha do Vulkan.</summary>
    public const string FALLBACK_FROM_ENV = "TAKTCHASE_FALLBACK_FROM";

    /// <summary>Motivo resumido da falha que originou o fallback (texto livre).</summary>
    public const string FALLBACK_REASON_ENV = "TAKTCHASE_FALLBACK_REASON";

    public static LaunchOptions Default => new LaunchOptions(RendererRequest.Auto, false, false, null, null, Array.Empty<string>());

    public RendererRequest Renderer { get; }

    /// <summary>Mostra o indicador de FPS/frametime já na abertura (<c>--show-fps</c>).</summary>
    public bool ShowFps { get; }

    /// <summary>Desativa o reinício automático com OpenGL quando o Vulkan falha (<c>--no-fallback</c>).</summary>
    public bool NoFallback { get; }

    /// <summary>Backend que falhou, quando esta execução é um fallback; caso contrário, nulo.</summary>
    public string? FallbackFrom { get; }

    public string? FallbackReason { get; }

    /// <summary>Problemas na linha de comando. Nunca são ignorados em silêncio: vão para o log e para o painel.</summary>
    public IReadOnlyList<string> Warnings { get; }

    public bool IsFallbackRun => FallbackFrom != null;

    private LaunchOptions(RendererRequest renderer, bool showFps, bool noFallback, string? fallbackFrom, string? fallbackReason, IReadOnlyList<string> warnings)
    {
        Renderer = renderer;
        ShowFps = showFps;
        NoFallback = noFallback;
        FallbackFrom = fallbackFrom;
        FallbackReason = fallbackReason;
        Warnings = warnings;
    }

    public static LaunchOptions Parse(IReadOnlyList<string> args, Func<string, string?> getEnvironmentVariable)
    {
        var renderer = RendererRequest.Auto;
        bool showFps = false;
        bool noFallback = false;
        var warnings = new List<string>();

        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];

            if (arg.Equals("--show-fps", StringComparison.OrdinalIgnoreCase))
                showFps = true;
            else if (arg.Equals("--no-fallback", StringComparison.OrdinalIgnoreCase))
                noFallback = true;
            else if (arg.StartsWith("--renderer=", StringComparison.OrdinalIgnoreCase))
                renderer = parseRenderer(arg.Substring("--renderer=".Length), warnings);
            else if (arg.Equals("--renderer", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Count)
                    renderer = parseRenderer(args[++i], warnings);
                else
                    warnings.Add("--renderer sem valor; usando auto.");
            }
            else
                warnings.Add($"Argumento desconhecido ignorado: {arg}");
        }

        string? fallbackFrom = emptyToNull(getEnvironmentVariable(FALLBACK_FROM_ENV));
        string? fallbackReason = emptyToNull(getEnvironmentVariable(FALLBACK_REASON_ENV));

        return new LaunchOptions(renderer, showFps, noFallback, fallbackFrom, fallbackReason, warnings);
    }

    private static RendererRequest parseRenderer(string value, List<string> warnings)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "auto":
                return RendererRequest.Auto;

            case "opengl":
            case "gl":
                return RendererRequest.OpenGL;

            case "vulkan":
            case "vk":
                return RendererRequest.Vulkan;

            default:
                warnings.Add($"Valor inválido para --renderer: '{value}'. Valores aceitos: auto, opengl, vulkan. Usando auto.");
                return RendererRequest.Auto;
        }
    }

    private static string? emptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
