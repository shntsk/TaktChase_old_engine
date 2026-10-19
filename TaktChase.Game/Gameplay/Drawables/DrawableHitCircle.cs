using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Judgements;
using TaktChase.Game.Gameplay.Skin;
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
    private readonly Container approach;
    private readonly Container body;
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

        // The containers exist from construction so UpdateTime/ApplyResult are always safe; load() fills them with the skin
        // artwork (or, if a texture is missing, with the original plain shapes).
        InternalChildren = new Drawable[]
        {
            body = new Container { RelativeSizeAxes = Axes.Both },
            approach = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Alpha = 0,
            },
        };
    }

    [BackgroundDependencyLoader]
    private void load(TextureStore textures)
    {
        float diameter = (float)difficulty.CircleRadius * 2;
        float scale = LiteSkinLayout.ScaleForDiameter(diameter);

        var circleTexture = textures.Get(LiteSkinLayout.HIT_CIRCLE_TEXTURE);
        var approachTexture = textures.Get(LiteSkinLayout.APPROACH_CIRCLE_TEXTURE);

        if (circleTexture != null)
            addSkinnedBody(textures, circleTexture, scale);
        else
            addFallbackBody(diameter);

        if (approachTexture != null)
        {
            float size = LiteSkinLayout.APPROACH_IMAGE_SIZE * scale;
            approach.Add(new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = approachTexture,
                Size = new Vector2(size),
            });
        }
        else
            addFallbackApproach();
    }

    private void addSkinnedBody(TextureStore textures, Texture circleTexture, float scale)
    {
        float size = LiteSkinLayout.HIT_CIRCLE_IMAGE_SIZE * scale;

        body.Add(new Sprite
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Texture = circleTexture,
            Size = new Vector2(size),
        });

        var numbers = new Container { Anchor = Anchor.Centre, Origin = Anchor.Centre };
        int number = Math.Max(0, HitObject.Number);
        float numberScale = scale * LiteSkinLayout.NumberFitScale(number);

        foreach (var digit in LiteSkinLayout.LayoutNumber(number))
        {
            var texture = textures.Get(LiteSkinLayout.DigitTexture(digit.Digit));
            if (texture == null)
                continue;

            numbers.Add(new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = texture,
                X = digit.CentreX * numberScale,
                Size = new Vector2(digit.Width * numberScale, LiteSkinLayout.DIGIT_IMAGE_HEIGHT * numberScale),
            });
        }

        body.Add(numbers);
    }

    private void addFallbackBody(float diameter)
    {
        body.Add(new CircularContainer
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            BorderThickness = diameter * 0.06f,
            BorderColour = style.RingColour,
            Children = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = style.BodyColour },
                new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = HitObject.Number.ToString(),
                    Font = FontUsage.Default.With(size: diameter * 0.5f, weight: "Bold"),
                    Colour = TaktPalette.TEXT_PRIMARY,
                },
            },
        });
    }

    private void addFallbackApproach()
    {
        approach.Add(new CircularContainer
        {
            RelativeSizeAxes = Axes.Both,
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Masking = true,
            BorderThickness = 3,
            BorderColour = style.ApproachColour,
            Child = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0, AlwaysPresent = true },
        });
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
