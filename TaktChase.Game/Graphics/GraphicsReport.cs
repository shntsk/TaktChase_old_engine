using System;
using System.Collections.Generic;
using System.Text;

namespace TaktChase.Game.Graphics;

/// <summary>
/// Relatório de diagnóstico gráfico. Só contém valores lidos do host/renderer do framework,
/// do ambiente de execução ou do log do framework. Valores indisponíveis aparecem como "n/d".
/// </summary>
public sealed class GraphicsReport
{
    public const string NOT_AVAILABLE = "n/d";

    public static GraphicsReport Unavailable { get; } = new GraphicsReport(
        new[] { new KeyValuePair<string, string>("Backend", "indisponível (sem host gráfico)") },
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        "indisponível",
        false);

    public IReadOnlyList<KeyValuePair<string, string>> Entries { get; }

    /// <summary>Avisos importantes: divergência pedido x ativo, fallback, argumentos inválidos.</summary>
    public IReadOnlyList<string> Alerts { get; }

    /// <summary>Linhas do log do framework com informações gráficas (versão GL, renderer, vendor etc.).</summary>
    public IReadOnlyList<string> FrameworkLogLines { get; }

    public IReadOnlyList<string> StartupErrors { get; }

    /// <summary>Texto curto do backend ATIVO, para a interface (ex.: "OpenGL · GLRenderer").</summary>
    public string BackendSummary { get; }

    /// <summary>Verdadeiro se há algo que o usuário deve notar (fallback, divergência, erro).</summary>
    public bool NeedsAttention { get; }

    public GraphicsReport(
        IReadOnlyList<KeyValuePair<string, string>> entries,
        IReadOnlyList<string> alerts,
        IReadOnlyList<string> frameworkLogLines,
        IReadOnlyList<string> startupErrors,
        string backendSummary,
        bool needsAttention)
    {
        Entries = entries;
        Alerts = alerts;
        FrameworkLogLines = frameworkLogLines;
        StartupErrors = startupErrors;
        BackendSummary = backendSummary;
        NeedsAttention = needsAttention;
    }

    /// <param name="maxLogLines">Limite de linhas de log incluídas; use um valor menor na interface e o completo no arquivo.</param>
    public string ToText(int maxLogLines = int.MaxValue)
    {
        var text = new StringBuilder();

        foreach (string alert in Alerts)
            text.AppendLine("! " + alert);

        if (Alerts.Count > 0)
            text.AppendLine();

        foreach (var entry in Entries)
            text.AppendLine($"{entry.Key}: {entry.Value}");

        if (FrameworkLogLines.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Log do framework (linhas gráficas):");

            int shown = Math.Min(FrameworkLogLines.Count, maxLogLines);

            for (int i = 0; i < shown; i++)
                text.AppendLine("  " + FrameworkLogLines[i]);

            if (shown < FrameworkLogLines.Count)
                text.AppendLine($"  … mais {FrameworkLogLines.Count - shown} linha(s) em diagnostics.txt");
        }

        if (StartupErrors.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Erros registrados na inicialização:");

            foreach (string error in StartupErrors)
                text.AppendLine("  " + error);
        }

        return text.ToString().TrimEnd();
    }
}
