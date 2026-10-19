using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using osu.Framework;
using osu.Framework.Platform;

namespace TaktChase.Game.Startup;

/// <summary>
/// Orchestrates backend selection BEFORE the graphics host is created, runs the game and, if an
/// explicitly requested Vulkan renderer fails during initialization, restarts the process with OpenGL.
/// </summary>
/// <remarks>
/// Why the fallback is a process restart: with the official environment variables the framework
/// fails permanently (no internal fallback), and the variables are read only once per
/// process (static constructor of FrameworkEnvironment). The "auto" mode uses the framework's own
/// fallback and does not go through here. See the README section "Fallback and limitations".
/// </remarks>
public static class TaktChaseLauncher
{
    public const string GAME_NAME = "TaktChase";

    public static int Run(string[] args)
    {
        var options = LaunchOptions.Parse(args, Environment.GetEnvironmentVariable);
        var notes = new List<string>();

        // 1) Backend selection: official framework variables, set before any call into the
        //    framework (they are read in the static constructor of FrameworkEnvironment).
        if (options.Renderer == RendererRequest.Auto && RendererEnvironment.HasExternalOverride(Environment.GetEnvironmentVariable))
        {
            notes.Add($"{RendererEnvironment.RENDERER_VARIABLE}/{RendererEnvironment.SURFACE_VARIABLE} were already set in the environment; "
                      + "the framework uses them without fallback, even with --renderer=auto.");
        }

        foreach (var variable in RendererEnvironment.GetVariables(options.Renderer))
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);

        RuntimeLogCapture.Attach();

        // 2) Vulkan pre-check (diagnostics only) when Vulkan was explicitly requested.
        VulkanProbeResult? probe = options.Renderer == RendererRequest.Vulkan ? VulkanProbe.RunNative() : null;

        StartupState.Configure(options, probe, notes);

        // 3) Host and game, always through the framework's own mechanisms.
        //    The host is disposed (using) BEFORE any failure handling, to release the single
        //    game instance before a possible process restart.
        try
        {
            var hostOptions = new HostOptions { FriendlyGameName = GAME_NAME };
            bool noUsableRenderer;

            using (GameHost host = Host.GetSuitableDesktopHost(GAME_NAME, hostOptions))
            using (osu.Framework.Game game = new TaktChaseGame(options))
            {
                host.Run(game);

                // GameHost.Run does NOT throw when automatic selection exhausts every renderer:
                // the framework logs "No usable renderer was found!" (LogLevel.Error), the host is left without
                // a Window and a Renderer, and Run simply returns. Without this check the process would exit
                // with code 0 and no diagnostics.
                noUsableRenderer = !StartupState.Completed && host.Renderer == null;
            }

            if (noUsableRenderer)
            {
                return handleStartupFailure(options, probe,
                    new InvalidOperationException("The framework ended initialization without a usable renderer (no window or renderer was created)."));
            }

            return 0;
        }
        catch (Exception e) when (!StartupState.Completed)
        {
            return handleStartupFailure(options, probe, e);
        }
    }

    private static int handleStartupFailure(LaunchOptions options, VulkanProbeResult? probe, Exception exception)
    {
        string reason = StartupFailure.Summarize(exception);
        string diagnosis = StartupFailure.BuildDiagnosis(options, reason, probe, RuntimeLogCapture.GetErrorLines());
        string? logPath = writeFailureLog(options, diagnosis, exception);

        Console.Error.WriteLine(diagnosis);

        if (StartupFailure.ShouldOfferOpenGLFallback(options))
        {
            if (relaunchWithOpenGL(options, reason))
                return 0;

            diagnosis += Environment.NewLine + "Could not automatically restart with OpenGL.";
            Console.Error.WriteLine("Could not automatically restart with OpenGL.");
        }

        showErrorDialog(logPath == null ? diagnosis : diagnosis + Environment.NewLine + "Log: " + logPath);
        return 2;
    }

    private static bool relaunchWithOpenGL(LaunchOptions options, string reason)
    {
        try
        {
            string? executable = Environment.ProcessPath;

            if (executable == null)
                return false;

            var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false };

            // Launched through "dotnet TaktChase.Desktop.dll": the first argument is the DLL itself.
            if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                string[] commandLine = Environment.GetCommandLineArgs();

                if (commandLine.Length > 0)
                    startInfo.ArgumentList.Add(commandLine[0]);
            }

            startInfo.ArgumentList.Add("--renderer=opengl");

            if (options.ShowFps)
                startInfo.ArgumentList.Add("--show-fps");

            if (options.AudioOffset != 0)
                startInfo.ArgumentList.Add("--audio-offset=" + options.AudioOffset.ToString("R", CultureInfo.InvariantCulture));

            startInfo.Environment[LaunchOptions.FALLBACK_FROM_ENV] = "Vulkan";
            startInfo.Environment[LaunchOptions.FALLBACK_REASON_ENV] = reason;

            Process.Start(startInfo)?.Dispose();
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Failed to restart with OpenGL: {e.GetType().Name}: {e.Message}");
            return false;
        }
    }

    private static string? writeFailureLog(LaunchOptions options, string diagnosis, Exception exception)
    {
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), GAME_NAME, "logs");
            Directory.CreateDirectory(directory);

            // A fallback run that also fails must NOT overwrite the log of the original failure.
            string path = Path.Combine(directory, options.IsFallbackRun ? "startup-failure-fallback.log" : "startup-failure.log");
            File.WriteAllText(path, $"{DateTime.Now:O}{Environment.NewLine}{diagnosis}{Environment.NewLine}{exception}{Environment.NewLine}");
            return path;
        }
        catch
        {
            return null;
        }
    }

    // A WinExe application has no console on Windows: without this the error would be invisible.
    private static void showErrorDialog(string message)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            MessageBoxW(IntPtr.Zero, message, GAME_NAME, 0x10 /* MB_ICONERROR */);
        }
        catch
        {
            // No dialog available; the diagnosis was already written to stderr and to the log file.
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
