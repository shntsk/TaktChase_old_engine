using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace TaktChase.Game.UI;

/// <summary>
/// Discreet FPS and frame-time indicator, off by default.
/// Reads the clock of the framework's update thread (Clock.FramesPerSecond / ElapsedFrameTime)
/// and only rewrites its text four times per second, to avoid allocations on every frame.
/// For per-thread statistics (Update/Draw/Input/Audio), use the framework's own overlay (Ctrl+F11).
/// </summary>
public partial class FpsCounter : Container
{
    private const double update_interval_ms = 250;

    private readonly bool initiallyVisible;
    private SpriteText text = null!;
    private double nextUpdate;
    private bool visible;

    public FpsCounter(bool initiallyVisible = false)
    {
        this.initiallyVisible = initiallyVisible;
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        visible = initiallyVisible;

        AutoSizeAxes = Axes.Both;
        Masking = true;
        CornerRadius = 12;
        Alpha = visible ? 1 : 0;

        Children = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = new Color4(0, 0, 0, 140),
            },
            text = new SpriteText
            {
                Padding = new MarginPadding { Horizontal = 14, Vertical = 8 },
                Font = FontUsage.Default.With(size: 22, fixedWidth: true),
                Colour = TaktPalette.TEXT_PRIMARY,
                Text = "-- fps",
            },
        };
    }

    public void ToggleVisibility()
    {
        visible = !visible;
        this.FadeTo(visible ? 1 : 0, 150, Easing.OutQuint);
    }

    protected override void Update()
    {
        base.Update();

        if (!visible)
            return;

        double now = Time.Current;

        if (now < nextUpdate)
            return;

        nextUpdate = now + update_interval_ms;
        text.Text = $"{Clock.FramesPerSecond:0} fps  {Clock.ElapsedFrameTime:0.0} ms (upd)";
    }
}
