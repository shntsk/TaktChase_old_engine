using System;
using System.Collections.Generic;
using System.Text;
using TaktChase.Game.Graphics;

namespace TaktChase.Game.Startup;

/// <summary>
/// Builds the readable diagnosis shown when the framework host cannot initialize the renderer.
/// Pure logic (no framework dependency), covered by tests.
/// </summary>
public static class StartupFailure
{
    private const int max_summary_length = 400;

    /// <summary>One-line summary of the exception, including the root cause when it differs.</summary>
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

    /// <summary>True if offering the automatic OpenGL restart makes sense.</summary>
    public static bool ShouldOfferOpenGLFallback(LaunchOptions options)
        => options.Renderer == RendererRequest.Vulkan && !options.NoFallback && !options.IsFallbackRun;

    public static string BuildDiagnosis(LaunchOptions options, string reason, VulkanProbeResult? probe, IReadOnlyList<string> frameworkErrors)
    {
        var text = new StringBuilder();

        text.AppendLine("TaktChase: startup failure. The renderer could not be initialized.");
        text.AppendLine($"Requested renderer: {options.Renderer}");
        text.AppendLine($"Failure reason: {reason}");

        // This run may be the restart of a fallback: the cause of the ORIGINAL failure must not be lost.
        if (options.IsFallbackRun)
            text.AppendLine($"Original failure ({options.FallbackFrom}, which caused this restart): {options.FallbackReason ?? GraphicsReport.NOT_AVAILABLE}");

        if (probe != null)
        {
            text.AppendLine();
            text.AppendLine("Vulkan pre-check:");

            foreach (string line in probe.ToLines())
                text.AppendLine("  " + line);
        }

        if (frameworkErrors.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Errors logged by the framework during initialization:");

            foreach (string line in frameworkErrors)
                text.AppendLine("  " + line);
        }

        text.AppendLine();

        switch (options.Renderer)
        {
            case RendererRequest.Vulkan:
                text.AppendLine("Check the Vulkan loader and the GPU driver (for example, by running vulkaninfo).");

                if (ShouldOfferOpenGLFallback(options))
                    text.AppendLine("Fallback: the game will restart with --renderer=opengl. Use --no-fallback to disable this.");
                else if (options.IsFallbackRun)
                    text.AppendLine("This run was already a fallback; no further restart will be attempted.");
                else
                    text.AppendLine("Fallback disabled (--no-fallback). Restart with --renderer=opengl or --renderer=auto.");

                break;

            case RendererRequest.OpenGL:
                text.AppendLine("Check the GPU's OpenGL driver. Try --renderer=auto to use the framework's own selection.");
                break;

            default:
                text.AppendLine("The framework went through its own fallback order and no renderer initialized.");
                break;
        }

        return text.ToString();
    }
}
