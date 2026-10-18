using System;
using System.Collections.Generic;
using System.Text;

namespace TaktChase.Game.Graphics;

/// <summary>
/// Graphics diagnostics report. It only contains values read from the framework host/renderer,
/// the runtime environment, or the framework log. Anything the public API does not expose is reported as "Unavailable".
/// </summary>
public sealed class GraphicsReport
{
    public const string NOT_AVAILABLE = "Unavailable";

    public static GraphicsReport Unavailable { get; } = new GraphicsReport(
        new[] { new KeyValuePair<string, string>("Graphics backend", "Unavailable (no graphics host)") },
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        NOT_AVAILABLE,
        false);

    public IReadOnlyList<KeyValuePair<string, string>> Entries { get; }

    /// <summary>Important warnings: requested vs. active renderer mismatch, fallback, invalid arguments.</summary>
    public IReadOnlyList<string> Alerts { get; }

    /// <summary>Framework log lines with graphics information (GL version, renderer, vendor, and so on).</summary>
    public IReadOnlyList<string> FrameworkLogLines { get; }

    public IReadOnlyList<string> StartupErrors { get; }

    /// <summary>Short text for the ACTIVE graphics backend, shown in the UI (for example "GLRenderer").</summary>
    public string BackendSummary { get; }

    /// <summary>True if there is something the user should notice (fallback, mismatch, error).</summary>
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

    /// <param name="maxLogLines">Maximum number of log lines to include; use a smaller value in the UI and the full set in the file.</param>
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
            text.AppendLine("Framework log (graphics lines):");

            int shown = Math.Min(FrameworkLogLines.Count, maxLogLines);

            for (int i = 0; i < shown; i++)
                text.AppendLine("  " + FrameworkLogLines[i]);

            if (shown < FrameworkLogLines.Count)
                text.AppendLine($"  … {FrameworkLogLines.Count - shown} more line(s) in diagnostics.txt");
        }

        if (StartupErrors.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Errors logged during startup:");

            foreach (string error in StartupErrors)
                text.AppendLine("  " + error);
        }

        return text.ToString().TrimEnd();
    }
}
