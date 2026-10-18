using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Platform;
using TaktChase.Game.Startup;

namespace TaktChase.Game.Graphics;

/// <summary>How the requested renderer compares with what the initialized host reports.</summary>
public enum RequestVerification
{
    /// <summary>Nothing to compare: the framework chose the renderer (<c>--renderer=auto</c>).</summary>
    NotApplicable,

    /// <summary>The active renderer type is the one the request produces, and that type identifies the backend on its own.</summary>
    Consistent,

    /// <summary>The active renderer type is the expected one, but it can drive several backends and the public API does not say which.</summary>
    PartiallyVerified,

    /// <summary>The active renderer type is not the one the request produces.</summary>
    Mismatch,

    /// <summary>The host did not report a renderer type, so the request could not be checked.</summary>
    Unavailable,
}

/// <summary>
/// Builds the graphics report from the already initialized framework host.
/// The active backend is ALWAYS what the host reports (<c>RendererInfo</c> and the renderer type name),
/// never just the argument that was received.
/// </summary>
/// <remarks>
/// Only members that are public in ppy.osu.Framework are read: <c>GameHost.RendererInfo</c>, <c>GameHost.Renderer</c>
/// (its type name only), <c>GameHost.Window</c> (presence only), and <c>GameHost.ResolvedRenderer</c>.
/// <c>IWindow.GraphicsSurface</c> and <c>IRenderer.VerticalSync</c> are not accessible from other assemblies, and they are
/// not reached through reflection. The graphics surface and the VSync state are therefore reported as unavailable
/// instead of being inferred from the command-line argument.
/// </remarks>
public static class GraphicsDiagnostics
{
    public const string SURFACE_UNAVAILABLE = "Unavailable (the graphics surface is not exposed by the public API of ppy.osu.Framework)";
    public const string VSYNC_UNAVAILABLE = "Unavailable (the renderer's VSync state is not exposed by the public API of ppy.osu.Framework)";

    public const string OPENGL_RENDERER_TYPE = "GLRenderer";
    public const string VELDRID_RENDERER_TYPE = "VeldridRenderer";

    /// <summary>Reads the host and builds the report. This is the only method that touches the framework host.</summary>
    public static GraphicsReport Capture(GameHost host, LaunchOptions options, VulkanProbeResult? probe, IReadOnlyList<string> notes)
    {
        bool selectedViaEnvironment = RendererEnvironment.HasExternalOverride(Environment.GetEnvironmentVariable);

        var snapshot = new GraphicsSnapshot
        {
            RendererInfo = host.RendererInfo,
            RendererTypeName = host.Renderer?.GetType().Name,
            HasWindow = host.Window != null,
            SelectedViaEnvironment = selectedViaEnvironment,
            ResolvedRenderer = selectedViaEnvironment ? null : host.ResolvedRenderer.ToString(),
            IsWindows = OperatingSystem.IsWindows(),
        };

        return Build(snapshot, options, probe, notes, RuntimeLogCapture.GetGraphicsLines(), RuntimeLogCapture.GetErrorLines());
    }

    /// <summary>Compares the requested renderer with the renderer type the host reports.</summary>
    public static RequestVerification VerifyRequest(RendererRequest request, string? activeRendererType)
    {
        if (request == RendererRequest.Auto)
            return RequestVerification.NotApplicable;

        if (string.IsNullOrWhiteSpace(activeRendererType))
            return RequestVerification.Unavailable;

        switch (request)
        {
            case RendererRequest.OpenGL:
                return activeRendererType.Equals(OPENGL_RENDERER_TYPE, StringComparison.Ordinal)
                    ? RequestVerification.Consistent
                    : RequestVerification.Mismatch;

            case RendererRequest.Vulkan:
                // VeldridRenderer can drive Vulkan, Direct3D 11, or Metal; the surface decides, and it is not public.
                return activeRendererType.Equals(VELDRID_RENDERER_TYPE, StringComparison.Ordinal)
                    ? RequestVerification.PartiallyVerified
                    : RequestVerification.Mismatch;

            default:
                return RequestVerification.NotApplicable;
        }
    }

    /// <summary>Pure report construction. Contains no framework calls, so it is covered by unit tests.</summary>
    public static GraphicsReport Build(
        GraphicsSnapshot snapshot,
        LaunchOptions options,
        VulkanProbeResult? probe,
        IReadOnlyList<string> notes,
        IReadOnlyList<string> frameworkLogLines,
        IReadOnlyList<string> errorLines)
    {
        var entries = new List<KeyValuePair<string, string>>();
        var alerts = new List<string>();

        string rendererInfo = string.IsNullOrWhiteSpace(snapshot.RendererInfo) ? GraphicsReport.NOT_AVAILABLE : snapshot.RendererInfo;
        string? rendererTypeName = string.IsNullOrWhiteSpace(snapshot.RendererTypeName) ? null : snapshot.RendererTypeName;
        string rendererType = rendererTypeName ?? GraphicsReport.NOT_AVAILABLE;
        var verification = VerifyRequest(options.Renderer, rendererTypeName);

        add(entries, "TaktChase", typeof(GraphicsReport).Assembly.GetName().Version?.ToString() ?? GraphicsReport.NOT_AVAILABLE);
        add(entries, "Requested renderer", options.Renderer.ToString());
        add(entries, "Selection", snapshot.SelectedViaEnvironment
            ? "OSU_GRAPHICS_* environment variables (no internal framework fallback)"
            : "Automatic framework selection (using the framework's fallback order)");
        add(entries, "Active renderer (host.RendererInfo)", rendererInfo);
        add(entries, "Renderer type", rendererType);
        add(entries, "Request check", describe(verification, rendererType));
        add(entries, "Window", snapshot.HasWindow ? "Present" : "Absent (headless)");
        add(entries, "Graphics surface", SURFACE_UNAVAILABLE);
        add(entries, "ResolvedRenderer (automatic mode)", snapshot.SelectedViaEnvironment
            ? "Not applicable (selected through environment variables)"
            : snapshot.ResolvedRenderer ?? GraphicsReport.NOT_AVAILABLE);
        add(entries, "Renderer VSync", VSYNC_UNAVAILABLE);

        if (options.Renderer == RendererRequest.Auto && !snapshot.SelectedViaEnvironment
            && snapshot.IsWindows && rendererTypeName == VELDRID_RENDERER_TYPE)
        {
            add(entries, "Automatic selection note",
                "On Windows the framework's automatic order prefers Direct3D 11 (through Veldrid), but the graphics surface is not exposed publicly, so it cannot be confirmed here. "
                + "Use --renderer=opengl or --renderer=vulkan to compare.");
        }

        add(entries, "Fallback", options.IsFallbackRun
            ? $"Yes — {options.FallbackFrom} failed during initialization"
            : "No");

        if (options.FallbackReason != null)
            add(entries, "Fallback reason", options.FallbackReason);

        if (probe != null)
        {
            foreach (string line in probe.ToLines())
                add(entries, "Vulkan pre-check", line);
        }

        add(entries, "System", RuntimeInformation.OSDescription);
        add(entries, "Architecture", RuntimeInformation.ProcessArchitecture.ToString());
        add(entries, ".NET", RuntimeInformation.FrameworkDescription);
        add(entries, "Logical processors", Environment.ProcessorCount.ToString());

        // Alerts: always compare the request with what the host actually initialized.
        switch (verification)
        {
            case RequestVerification.Mismatch when options.Renderer == RendererRequest.Vulkan:
                alerts.Add($"Vulkan was requested ({VELDRID_RENDERER_TYPE}), but the active renderer type is '{rendererType}'.");
                break;

            case RequestVerification.Mismatch:
                alerts.Add($"OpenGL was requested ({OPENGL_RENDERER_TYPE}), but the active renderer type is '{rendererType}'.");
                break;

            case RequestVerification.Unavailable:
                alerts.Add($"{options.Renderer} was requested, but the host reported no active renderer type, so the request could not be verified.");
                break;
        }

        if (options.IsFallbackRun)
        {
            alerts.Add($"Fallback: {options.FallbackFrom} failed to initialize; this run uses the '{rendererType}' renderer."
                       + (options.FallbackReason != null ? $" Fallback reason: {options.FallbackReason}" : string.Empty));
        }

        if (probe is { LooksUsable: false })
            alerts.Add("The Vulkan pre-check found a problem in the environment (see the lines above).");

        foreach (string warning in options.Warnings)
            alerts.Add(warning);

        foreach (string note in notes)
            alerts.Add(note);

        string summary = rendererType;
        bool needsAttention = options.IsFallbackRun || alerts.Count > 0 || errorLines.Count > 0;

        return new GraphicsReport(entries, alerts, frameworkLogLines, errorLines, summary, needsAttention);
    }

    private static string describe(RequestVerification verification, string rendererType)
    {
        switch (verification)
        {
            case RequestVerification.Consistent:
                return $"Consistent with the request ({rendererType} is the OpenGL renderer)";

            case RequestVerification.PartiallyVerified:
                return $"Partially verified: {rendererType} is active, but it can drive several backends and the graphics surface is not exposed publicly, so Vulkan is not confirmed";

            case RequestVerification.Mismatch:
                return $"Mismatch: the active renderer type '{rendererType}' is not the one this request produces";

            case RequestVerification.Unavailable:
                return "Unavailable (the host reported no active renderer type)";

            default:
                return "Not applicable (the framework chose the renderer)";
        }
    }

    private static void add(List<KeyValuePair<string, string>> entries, string key, string value)
        => entries.Add(new KeyValuePair<string, string>(key, value));
}
