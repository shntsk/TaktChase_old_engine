using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using TaktChase.Game.Graphics;
using TaktChase.Game.UI;

namespace TaktChase.Game.Screens;

/// <summary>
/// Tela inicial da fundação: identidade visual, pulso de ritmo e o backend ativo.
/// Usa apenas drawables do framework (sem texturas além da marca) e transformações leves.
/// </summary>
public partial class HomeScreen : Screen
{
    private const double beat_interval_ms = 1000;

    private readonly GraphicsReport report;
    private Drawable ring = null!;
    private Drawable dot = null!;

    public HomeScreen(GraphicsReport report)
    {
        this.report = report;
    }

    [BackgroundDependencyLoader]
    private void load(TextureStore textures)
    {
        InternalChildren = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = TaktPalette.BACKGROUND,
            },
            new Sprite
            {
                Texture = textures.Get("taktchase-mark"),
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                Margin = new MarginPadding(56),
                Size = new Vector2(56),
                FillMode = FillMode.Fit,
            },
            new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 26),
                Children = new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(380),
                        Children = new Drawable[]
                        {
                            ring = new CircularContainer
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(150),
                                Masking = true,
                                BorderThickness = 7,
                                BorderColour = TaktPalette.CHASE,
                                Alpha = 0,
                                Child = new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                    AlwaysPresent = true,
                                },
                            },
                            dot = new Circle
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(58),
                                Colour = TaktPalette.TARGET,
                            },
                        },
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "TaktChase",
                        Font = FontUsage.Default.With(size: 104, weight: "Bold"),
                        Colour = TaktPalette.TEXT_PRIMARY,
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "Precisão no tempo. Foco no alvo. Leitura de cada objeto.",
                        Font = FontUsage.Default.With(size: 30),
                        Colour = TaktPalette.TEXT_SECONDARY,
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "FUNDAÇÃO  ·  ETAPA 0",
                        Font = FontUsage.Default.With(size: 20, weight: "Bold"),
                        Colour = TaktPalette.CHASE,
                        Spacing = new Vector2(5, 0),
                    },
                    createBackendChip(),
                },
            },
            new SpriteText
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Margin = new MarginPadding { Bottom = 44 },
                Text = "F2  FPS    ·    F3  diagnóstico gráfico    ·    Ctrl+F11  estatísticas do framework",
                Font = FontUsage.Default.With(size: 20),
                Colour = TaktPalette.TEXT_SECONDARY,
            },
        };
    }

    private Drawable createBackendChip() => new Container
    {
        Anchor = Anchor.TopCentre,
        Origin = Anchor.TopCentre,
        AutoSizeAxes = Axes.Both,
        Masking = true,
        CornerRadius = 26,
        BorderThickness = 1,
        BorderColour = TaktPalette.HAIRLINE,
        Children = new Drawable[]
        {
            new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = TaktPalette.SURFACE,
            },
            new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(14, 0),
                Padding = new MarginPadding { Horizontal = 28, Vertical = 14 },
                Children = new Drawable[]
                {
                    new Circle
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(12),
                        Colour = report.NeedsAttention ? TaktPalette.WARNING : TaktPalette.OK,
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = $"Backend ativo: {report.BackendSummary}",
                        Font = FontUsage.Default.With(size: 24),
                        Colour = TaktPalette.TEXT_PRIMARY,
                    },
                },
            },
        },
    };

    protected override void LoadComplete()
    {
        base.LoadComplete();

        pulse();
        Scheduler.AddDelayed(pulse, beat_interval_ms, true);
    }

    /// <summary>Um "compasso": o anel persegue o alvo e o alvo reage ao toque.</summary>
    private void pulse()
    {
        ring.FadeTo(0).ScaleTo(2.3f)
            .Then().FadeTo(0.9f, 180).ScaleTo(0.43f, 820, Easing.InCubic)
            .Then().FadeOut(120);

        dot.Delay(790).ScaleTo(1.3f, 30).Then().ScaleTo(1f, 320, Easing.OutQuint);
    }
}
