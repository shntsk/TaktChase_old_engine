using System;
using System.Collections.Generic;
using System.Text;

namespace TaktChase.Game.Startup;

/// <summary>
/// Monta o diagnóstico legível quando o host do framework não consegue inicializar o renderer.
/// Lógica pura (sem framework), coberta por testes.
/// </summary>
public static class StartupFailure
{
    private const int max_summary_length = 400;

    /// <summary>Resumo de uma linha da exceção, incluindo a causa raiz quando diferente.</summary>
    public static string Summarize(Exception exception)
    {
        Exception top = exception;

        while (top is AggregateException { InnerExceptions.Count: 1 } aggregate)
            top = aggregate.InnerExceptions[0];

        string summary = describe(top);
        Exception root = top.GetBaseException();

        if (!ReferenceEquals(root, top))
            summary += " <- " + describe(root);

        return summary.Length <= max_summary_length ? summary : summary.Substring(0, max_summary_length) + "…";
    }

    private static string describe(Exception exception)
    {
        string message = exception.Message;
        int newline = message.IndexOfAny(new[] { '\r', '\n' });

        if (newline >= 0)
            message = message.Substring(0, newline);

        return $"{exception.GetType().Name}: {message}";
    }

    /// <summary>Verdadeiro se faz sentido oferecer o reinício automático com OpenGL.</summary>
    public static bool ShouldOfferOpenGLFallback(LaunchOptions options)
        => options.Renderer == RendererRequest.Vulkan && !options.NoFallback && !options.IsFallbackRun;

    public static string BuildDiagnosis(LaunchOptions options, string reason, VulkanProbeResult? probe, IReadOnlyList<string> frameworkErrors)
    {
        var text = new StringBuilder();

        text.AppendLine("TaktChase: falha ao inicializar o renderer.");
        text.AppendLine($"Renderer solicitado: {options.Renderer}");
        text.AppendLine($"Motivo: {reason}");

        // Esta execução pode ser o reinício de um fallback: a causa da falha ORIGINAL não pode se perder.
        if (options.IsFallbackRun)
            text.AppendLine($"Falha original ({options.FallbackFrom}, que motivou este reinício): {options.FallbackReason ?? "n/d"}");

        if (probe != null)
        {
            text.AppendLine();
            text.AppendLine("Pré-verificação Vulkan:");

            foreach (string line in probe.ToLines())
                text.AppendLine("  " + line);
        }

        if (frameworkErrors.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Erros registrados pelo framework durante a inicialização:");

            foreach (string line in frameworkErrors)
                text.AppendLine("  " + line);
        }

        text.AppendLine();

        switch (options.Renderer)
        {
            case RendererRequest.Vulkan:
                text.AppendLine("Verifique o loader Vulkan e o driver da GPU (por exemplo, executando vulkaninfo).");

                if (ShouldOfferOpenGLFallback(options))
                    text.AppendLine("Fallback: o jogo será reiniciado com --renderer=opengl. Use --no-fallback para desativar.");
                else if (options.IsFallbackRun)
                    text.AppendLine("Esta execução já era um fallback; nenhum novo reinício será feito.");
                else
                    text.AppendLine("Fallback desativado (--no-fallback). Reinicie com --renderer=opengl ou --renderer=auto.");

                break;

            case RendererRequest.OpenGL:
                text.AppendLine("Verifique o driver OpenGL da GPU. Tente --renderer=auto para a seleção do framework.");
                break;

            default:
                text.AppendLine("O framework percorreu a própria ordem de fallback e nenhum renderer inicializou.");
                break;
        }

        return text.ToString();
    }
}
