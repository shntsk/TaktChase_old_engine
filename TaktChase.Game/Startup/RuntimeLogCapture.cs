using System.Collections.Generic;
using System.Text.RegularExpressions;
using osu.Framework.Logging;

namespace TaktChase.Game.Startup;

/// <summary>
/// Guarda, em memória e com limite fixo, linhas do log do framework relevantes para o diagnóstico
/// gráfico. O framework registra versão/renderer/vendor do OpenGL apenas no log (não há propriedade
/// pública), então esta é a forma suportada de mostrá-las sem inventar dados.
/// </summary>
public static class RuntimeLogCapture
{
    private const int max_graphics_lines = 48;
    private const int max_error_lines = 24;
    private const int max_line_length = 240;

    private static readonly object sync = new object();
    private static readonly List<string> graphics_lines = new List<string>();
    private static readonly List<string> error_lines = new List<string>();

    private static readonly Regex graphics_pattern = new Regex(
        @"\b(gl|opengl|glsl|vulkan|veldrid|direct3d|d3d|renderer|vendor|adapter|surface|backend)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool attached;

    public static void Attach()
    {
        lock (sync)
        {
            if (attached)
                return;

            attached = true;
        }

        Logger.NewEntry += onEntry;
    }

    public static IReadOnlyList<string> GetGraphicsLines()
    {
        lock (sync)
            return graphics_lines.ToArray();
    }

    public static IReadOnlyList<string> GetErrorLines()
    {
        lock (sync)
            return error_lines.ToArray();
    }

    private static void onEntry(LogEntry entry)
    {
        string? message = entry.Message;

        if (string.IsNullOrEmpty(message))
            return;

        // Evita duplicar as próprias linhas do TaktChase.
        if (message.StartsWith("[TaktChase]", System.StringComparison.Ordinal))
            return;

        string line = message.Length <= max_line_length ? message : message.Substring(0, max_line_length) + "…";

        lock (sync)
        {
            if (entry.Level == LogLevel.Error)
            {
                if (error_lines.Count < max_error_lines)
                    error_lines.Add(line);
            }
            else if ((entry.Target == null || entry.Target == LoggingTarget.Runtime)
                     && graphics_lines.Count < max_graphics_lines
                     && graphics_pattern.IsMatch(message))
            {
                graphics_lines.Add(line);
            }
        }
    }
}
