using System;
using System.Collections.Generic;

namespace TaktChase.Game.Startup;

/// <summary>
/// Startup state shared between the launcher and the game.
/// </summary>
public static class StartupState
{
    private static volatile bool completed;

    /// <summary>
    /// True once the window, the renderer, and the initial screen have been created successfully.
    /// Exceptions after this point are NOT treated as a renderer startup failure.
    /// </summary>
    public static bool Completed => completed;

    public static LaunchOptions Options { get; private set; } = LaunchOptions.Default;

    public static VulkanProbeResult? Probe { get; private set; }

    public static IReadOnlyList<string> Notes { get; private set; } = Array.Empty<string>();

    public static void Configure(LaunchOptions options, VulkanProbeResult? probe, IReadOnlyList<string> notes)
    {
        Options = options;
        Probe = probe;
        Notes = notes;
    }

    public static void MarkCompleted() => completed = true;
}
