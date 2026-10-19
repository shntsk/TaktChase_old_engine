using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;
using TaktChase.Game.Gameplay.Judgements;
using TaktChase.Game.UI;

namespace TaktChase.Game.Gameplay.Drawables;

/// <summary>
/// Shows how early (left) or late (right) each successful hit was, around the centre zero-timing line.
/// Bands show the 50, 100 and 300 windows; every hit adds a tick that fades away slowly.
/// </summary>
public partial class HitErrorBar : Container
{
    private const float bar_width = 560;
    private const float bar_height = 14;
    private const double tick_lifetime = 5000;

    private readonly JudgementWindows windows;
    private readonly Container ticks;

    public HitErrorBar(JudgementWindows windows)
    {
        this.windows = windows;

        Size = new Vector2(bar_width, 40);

        Children = new Drawable[]
        {
            band(windows.MehWindow, new Color4(255, 159, 10, 90)),
            band(windows.OkWindow, new Color4(48, 209, 88, 110)),
            band(windows.GreatWindow, new Color4(100, 210, 255, 150)),
            new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(2, 34),
                Colour = TaktPalette.TEXT_PRIMARY,
            },
            ticks = new Container { RelativeSizeAxes = Axes.Both },
        };
    }

    private Drawable band(double window, Color4 colour) => new Box
    {
        Anchor = Anchor.Centre,
        Origin = Anchor.Centre,
        Size = new Vector2((float)(window / windows.MehWindow) * bar_width, bar_height),
        Colour = colour,
    };

    /// <summary>Horizontal offset of a tick from the bar's centre, in pixels. Errors beyond the 50 window are clamped to the bar's edge.</summary>
    public static float TickOffset(double timingError, double mehWindow)
        => (float)Math.Clamp(timingError / mehWindow, -1, 1) * (bar_width / 2);

    /// <summary>Adds a tick for a successful hit. <paramref name="timingError"/>: negative = early.</summary>
    public void AddHit(double timingError, HitResult result)
    {
        var tick = new Box
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            X = TickOffset(timingError, windows.MehWindow),
            Size = new Vector2(3, 28),
            Colour = result switch
            {
                HitResult.Great => TaktPalette.CHASE,
                HitResult.Ok => TaktPalette.OK,
                _ => TaktPalette.WARNING,
            },
        };

        ticks.Add(tick);
        tick.FadeOut(tick_lifetime, Easing.In).Expire();
    }
}
