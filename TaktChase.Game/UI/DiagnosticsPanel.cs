using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using TaktChase.Game.Graphics;

namespace TaktChase.Game.UI;

/// <summary>
/// Painel de diagnóstico gráfico (tecla F3). Mostra o <see cref="GraphicsReport"/> e a memória do processo,
/// atualizada só enquanto o painel está visível.
/// </summary>
public partial class DiagnosticsPanel : Container
{
    private const int max_log_lines_in_panel = 10;
    private const double memory_update_interval_ms = 2000;

    private readonly GraphicsReport report;
    private SpriteText memoryText = null!;
    private double nextMemoryUpdate;
    private bool visible;

    public DiagnosticsPanel(GraphicsReport report)
    {
        this.report = report;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        Width = 900;
        AutoSizeAxes = Axes.Y;
        Masking = true;
        CornerRadius = 22;
        BorderThickness = 1;
        BorderColour = TaktPalette.HAIRLINE;
        Alpha = 0;

        Children = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = TaktPalette.SURFACE,
            },
            new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(32),
                Spacing = new Vector2(0, 16),
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = "Diagnóstico gráfico",
                        Font = FontUsage.Default.With(size: 32, weight: "Bold"),
                        Colour = TaktPalette.TEXT_PRIMARY,
                    },
                    new TextFlowContainer(t =>
                    {
                        t.Font = FontUsage.Default.With(size: 20);
                        t.Colour = TaktPalette.TEXT_PRIMARY;
                    })
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = report.ToText(max_log_lines_in_panel),
                    },
                    memoryText = new SpriteText
                    {
                        Font = FontUsage.Default.With(size: 20),
                        Colour = TaktPalette.TEXT_SECONDARY,
                        Text = "Memória: --",
                    },
                },
            },
        };
    }

    public void ToggleVisibility()
    {
        visible = !visible;
        nextMemoryUpdate = 0;
        this.FadeTo(visible ? 1 : 0, 150, Easing.OutQuint);
    }

    protected override void Update()
    {
        base.Update();

        if (!visible)
            return;

        double now = Time.Current;

        if (now < nextMemoryUpdate)
            return;

        nextMemoryUpdate = now + memory_update_interval_ms;

        long workingSet;

        using (var process = Process.GetCurrentProcess())
            workingSet = process.WorkingSet64;

        memoryText.Text = $"Memória: working set {workingSet / 1048576.0:0.0} MiB · heap GC {GC.GetTotalMemory(false) / 1048576.0:0.0} MiB · "
                          + $"coletas gen0/1/2: {GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}";
    }
}
