using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Judgements;
using TaktChase.Game.UI;

namespace TaktChase.Game.Gameplay.Drawables;

/// <summary>
/// Visual configuration of hit circles, kept apart from the beatmap data so the look can change without touching gameplay.
/// Timing (fade-in, approach duration) and size come from <see cref="BeatmapDifficulty"/>, not from here.
/// </summary>
public sealed class HitCircleStyle
{
    public static HitCircleStyle Default { get; } = new HitCircleStyle();

    /// <summary>Approach circle scale relative to the circle at the moment the object appears.</summary>
    public float ApproachStartScale { get; init; } = 4f;

    /// <summary>How long a successfully hit circle takes to disappear, in real milliseconds.</summary>
    public double HitFadeOutTime { get; init; } = 160;

    /// <summary>How long a missed circle takes to disappear, in real milliseconds.</summary>
    public double MissFadeOutTime { get; init; } = 260;

    /// <summary>Scale a hit circle grows to while fading out.</summary>
    public float HitEndScale { get; init; } = 1.2f;

    public Color4 BodyColour { get; init; } = TaktPalette.SURFACE;

    public Color4 RingColour { get; init; } = TaktPalette.TARGET;

    public Color4 ApproachColour { get; init; } = TaktPalette.CHASE;
}

/// <summary>
/// Draws one hit circle with its approach circle. Appearance before judgement is a pure function of the gameplay time it is
/// given (see <see cref="UpdateTime"/>), so it stays correct at any frame rate and freezes while the game is paused.
/// It makes no judgement decisions. The hit/miss animation after judgement runs in real time and is short.
/// </summary>
public partial class DrawableHitCircle : CompositeDrawable
{
    public HitCircle HitObject { get; }

    private readonly BeatmapDifficulty difficulty;
    private readonly HitCircleStyle style;
    private readonly Drawable approach;
    private readonly Drawable body;
    private readonly double appearTime;
    private bool judged;

    public DrawableHitCircle(HitCircle hitObject, BeatmapDifficulty difficulty, HitCircleStyle? style = null)
    {
        HitObject = hitObject;
        this.difficulty = difficulty;
        this.style = style ?? HitCircleStyle.Default;
        appearTime = hitObject.GetAppearTime(difficulty);

        float diameter = (float)difficulty.CircleRadius * 2;

        Size = new Vector2(diameter);
        Origin = Anchor.Centre;
        Position = hitObject.Position;
        Alpha = 0;

        InternalChildren = new Drawable[]
        {
            body = new CircularContainer
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                BorderThickness = diameter * 0.06f,
                BorderColour = this.style.RingColour,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = this.style.BodyColour },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = hitObject.Number.ToString(),
                        Font = FontUsage.Default.With(size: diameter * 0.5f, weight: "Bold"),
                        Colour = TaktPalette.TEXT_PRIMARY,
                    },
                },
            },
            approach = new CircularContainer
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Masking = true,
                BorderThickness = 3,
                BorderColour = this.style.ApproachColour,
                Alpha = 0,
                Child = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0, AlwaysPresent = true },
            },
        };
    }

    /// <summary>Positions the approach circle and fade-in for the given gameplay time. Ignored once the circle is judged.</summary>
    public void UpdateTime(double gameplayTime)
    {
        if (judged)
            return;

        Alpha = HitCircleAnimation.BodyAlpha(gameplayTime, appearTime, difficulty.FadeInTime);
        approach.Alpha = HitCircleAnimation.ApproachAlpha(gameplayTime, appearTime, difficulty.FadeInTime, difficulty.ApproachTime);
        approach.Scale = new Vector2(HitCircleAnimation.ApproachScale(gameplayTime, appearTime, difficulty.ApproachTime, style.ApproachStartScale));
    }

    /// <summary>Plays the result animation (real time) and expires the circle afterwards. Only the first call has an effect.</summary>
    public void ApplyResult(HitResult result)
    {
        if (judged)
            return;

        judged = true;
        approach.Hide();
        Alpha = 1;

        if (result.IsHit())
            this.ScaleTo(style.HitEndScale, style.HitFadeOutTime, Easing.OutQuint).FadeOut(style.HitFadeOutTime).Expire();
        else
        {
            body.FadeColour(TaktPalette.TEXT_SECONDARY, 80);
            this.FadeOut(style.MissFadeOutTime).Expire();
        }
    }
}
