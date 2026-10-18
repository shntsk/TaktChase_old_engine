using System;
using System.Collections.Generic;

namespace TaktChase.Game.Startup;

/// <summary>
/// TaktChase command-line options. It does not depend on the framework, so it can be
/// parsed BEFORE any graphics initialization and tested without a GPU.
/// </summary>
public sealed class LaunchOptions
{
    /// <summary>Set by the process that restarts the game after a Vulkan failure.</summary>
    public const string FALLBACK_FROM_ENV = "TAKTCHASE_FALLBACK_FROM";

    /// <summary>Short reason for the failure that triggered the fallback (free text).</summary>
    public const string FALLBACK_REASON_ENV = "TAKTCHASE_FALLBACK_REASON";

    public static LaunchOptions Default => new LaunchOptions(RendererRequest.Auto, false, false, null, null, Array.Empty<string>());

    public RendererRequest Renderer { get; }

    /// <summary>Shows the FPS/frame-time indicator at startup (<c>--show-fps</c>).</summary>
    public bool ShowFps { get; }

    /// <summary>Disables the automatic OpenGL restart when Vulkan fails (<c>--no-fallback</c>).</summary>
    public bool NoFallback { get; }

    /// <summary>The backend that failed when this run is a fallback; otherwise null.</summary>
    public string? FallbackFrom { get; }

    public string? FallbackReason { get; }

    /// <summary>Command-line problems. They are never silently ignored: they go to the log and the diagnostics panel.</summary>
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
                    warnings.Add("--renderer has no value; using auto.");
            }
            else
                warnings.Add($"Unknown argument ignored: {arg}");
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
                warnings.Add($"Invalid value for --renderer: '{value}'. Accepted values: auto, opengl, vulkan. Using auto.");
                return RendererRequest.Auto;
        }
    }

    private static string? emptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
