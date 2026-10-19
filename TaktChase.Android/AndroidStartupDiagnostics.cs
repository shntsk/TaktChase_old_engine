using System;
using System.IO;
using Android.Content;
using Android.OS;

namespace TaktChase.Android;

/// <summary>
/// Stores a small startup breadcrumb file in private app storage so the launcher can explain a failed launch
/// without requiring logcat. A native process crash may leave only the last completed stage.
/// </summary>
internal static class AndroidStartupDiagnostics
{
    private static readonly object sync = new();
    private static string? statePath;
    private static string currentStage = "Android process started";
    private static bool handlersInstalled;

    public static void Initialize(Context context)
    {
        lock (sync)
        {
            if (statePath == null)
            {
                try
                {
                    string? directory = context.FilesDir?.AbsolutePath;
                    if (!string.IsNullOrEmpty(directory))
                        statePath = Path.Combine(directory, "taktchase-startup-state.txt");
                }
                catch
                {
                    return;
                }
            }

            if (!handlersInstalled)
            {
                AppDomain.CurrentDomain.UnhandledException += onUnhandledException;
                handlersInstalled = true;
            }
        }
    }

    public static string? ReadPreviousFailure()
    {
        lock (sync)
        {
            try
            {
                if (statePath == null || !File.Exists(statePath))
                    return null;

                string report = File.ReadAllText(statePath);
                if (report.StartsWith("Status=STARTING", StringComparison.Ordinal)
                    || report.StartsWith("Status=ERROR", StringComparison.Ordinal))
                    return report;
            }
            catch
            {
                // Failure to read an old report must not prevent the game from launching.
            }

            return null;
        }
    }

    public static void BeginAttempt()
    {
        lock (sync)
        {
            currentStage = "LauncherActivity";
            writeLocked("STARTING", currentStage, null);
        }
    }

    public static void ReportStage(string stage)
    {
        lock (sync)
        {
            currentStage = stage;
            writeLocked(
                stage == "First game update completed" ? "RUNNING" : "STARTING",
                stage,
                null);
        }
    }

    private static void onUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        lock (sync)
        {
            string message = args.ExceptionObject?.ToString() ?? "The runtime reported an unknown unhandled exception.";
            writeLocked("ERROR", currentStage, message);
        }
    }

    private static void writeLocked(string status, string stage, string? exception)
    {
        if (statePath == null)
            return;

        string report = "Status=" + status + Environment.NewLine
                        + "TimestampUtc=" + DateTimeOffset.UtcNow.ToString("O") + Environment.NewLine
                        + "LastStage=" + stage + Environment.NewLine
                        + "AndroidVersion=" + Build.VERSION.Release + Environment.NewLine
                        + "ApiLevel=" + (int)Build.VERSION.SdkInt + Environment.NewLine;

        if (!string.IsNullOrEmpty(exception))
        {
            string bounded = exception.Length <= 12000 ? exception : exception.Substring(0, 12000);
            report += Environment.NewLine + "Exception:" + Environment.NewLine + bounded + Environment.NewLine;
        }

        string temporaryPath = statePath + ".tmp";

        try
        {
            File.WriteAllText(temporaryPath, report);
            File.Move(temporaryPath, statePath, true);
        }
        catch
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // Diagnostics are best-effort and must never take down the game.
            }
        }
    }
}
