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

        // O backend efetivo vem do host já inicializado, nunca do argumento recebido.
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

        screenStack.Push(new HomeScreen(report));

        // A partir daqui, falhas deixam de ser "falha de inicialização de renderer".
        StartupState.MarkCompleted();
    }

    protected override bool OnKeyDown(KeyDownEvent e)
    {
        // F2 e F3 sem modificadores não conflitam com os atalhos do framework (todos usam Ctrl/Alt).
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
        Logger.Log($"[TaktChase] Renderer solicitado: {options.Renderer}; backend efetivo: {report.BackendSummary}", LoggingTarget.Runtime, LogLevel.Important);

        foreach (string alert in report.Alerts)
            Logger.Log($"[TaktChase] Aviso: {alert}", LoggingTarget.Runtime, LogLevel.Important);

        if (options.IsFallbackRun)
            Logger.Log($"[TaktChase] Fallback de {options.FallbackFrom}. Motivo: {options.FallbackReason ?? "n/d"}", LoggingTarget.Runtime, LogLevel.Important);
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
            Logger.Log($"[TaktChase] Não foi possível gravar diagnostics.txt: {e.Message}", LoggingTarget.Runtime, LogLevel.Important);
        }
    }
}
