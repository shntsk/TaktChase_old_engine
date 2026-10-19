using System;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK;
using osuTK.Input;
using TaktChase.Game.Graphics;
using TaktChase.Game.Screens;
using TaktChase.Game.Startup;
using TaktChase.Game.UI;

namespace TaktChase.Game;

public partial class TaktChaseGame : TaktChaseGameBase
{
    private readonly LaunchOptions options;

    private ScreenStack screenStack = null!;
    private FpsCounter fpsCounter = null!;
    private DiagnosticsPanel diagnosticsPanel = null!;
    private GraphicsReport report = GraphicsReport.Unavailable;
    private GameHost gameHost = null!;
    private bool firstUpdateReported;

    public TaktChaseGame()
        : this(LaunchOptions.Default)
    {
    }

    public TaktChaseGame(LaunchOptions options)
    {
        this.options = options;
    }

    public override bool HandleNonPositionalInput => true;

    [BackgroundDependencyLoader]
    private void load(GameHost host)
    {
        gameHost = host;

        // The active backend comes from the already initialized host, never from the received argument.
        report = GraphicsDiagnostics.Capture(host, options, StartupState.Probe, StartupState.Notes);

        Children = new Drawable[]
        {
            screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both },
            fpsCounter = new FpsCounter(options.ShowFps)
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                Margin = new MarginPadding(28),
            },
            diagnosticsPanel = new DiagnosticsPanel(report)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            },
        };
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();

        if (gameHost.Window != null)
            gameHost.Window.Title = TaktChaseLauncher.GAME_NAME;

        logReport();
        writeReportFile();

        // Stage 1: launch straight into the demonstration gameplay (HomeScreen is kept but no longer shown).
        screenStack.Push(new GameplayScreen(options.AudioOffset));
        StartupState.ReportProgress("Initial gameplay screen created");

        // From here on, failures are no longer treated as a renderer startup failure.
        StartupState.MarkCompleted();
    }

    protected override void Update()
    {
        base.Update();

        if (firstUpdateReported)
            return;

        firstUpdateReported = true;
        StartupState.ReportProgress("First game update completed");
    }

    protected override bool OnKeyDown(KeyDownEvent e)
    {
        // Unmodified F2 and F3 do not conflict with the framework's shortcuts (they all use Ctrl/Alt).
        if (!e.Repeat && !e.ControlPressed && !e.AltPressed && !e.ShiftPressed)
        {
            switch (e.Key)
            {
                case Key.F2:
                    fpsCounter.ToggleVisibility();
                    return true;

                case Key.F3:
                    diagnosticsPanel.ToggleVisibility();
                    return true;
            }
        }

        return base.OnKeyDown(e);
    }

    private void logReport()
    {
        Logger.Log($"[TaktChase] Requested renderer: {options.Renderer}; graphics backend: {report.BackendSummary}", LoggingTarget.Runtime, LogLevel.Important);

        foreach (string alert in report.Alerts)
            Logger.Log($"[TaktChase] Warning: {alert}", LoggingTarget.Runtime, LogLevel.Important);

        if (options.IsFallbackRun)
            Logger.Log($"[TaktChase] Fallback from {options.FallbackFrom}. Fallback reason: {options.FallbackReason ?? GraphicsReport.NOT_AVAILABLE}", LoggingTarget.Runtime, LogLevel.Important);
    }

    private void writeReportFile()
    {
        try
        {
            string path = gameHost.Storage.GetFullPath("diagnostics.txt", true);
            File.WriteAllText(path, report.ToText() + Environment.NewLine);
        }
        catch (Exception e)
        {
            Logger.Log($"[TaktChase] Could not write diagnostics.txt: {e.Message}", LoggingTarget.Runtime, LogLevel.Important);
        }
    }
}
