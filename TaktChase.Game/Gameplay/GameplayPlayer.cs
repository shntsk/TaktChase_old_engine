using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osuTK;
using osuTK.Input;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Drawables;
using TaktChase.Game.Gameplay.Judgements;
using TaktChase.Game.Gameplay.Timing;
using TaktChase.Game.UI;

namespace TaktChase.Game.Gameplay;

/// <summary>
/// One play session: owns the audio track, the <see cref="GameplayClock"/>, the <see cref="GameplaySession"/> (and through it the
/// <see cref="GameplayEngine"/>), the playfield and the HUD, and connects framework input to them. All judging happens in the engine;
/// this class only translates framework events and draws.
/// </summary>
public partial class GameplayPlayer : Container
{
    private const double load_timeout_ms = 10000;

    private readonly Beatmap beatmap;
    private readonly double audioOffset;

    /// <summary>Raised when the player asks to play again (R).</summary>
    public Action? RestartRequested;

    private GameplayEngine engine = null!;
    private GameplaySession session = null!;
    private GameplayClock? clock;
    private ITrackStore trackStore = null!;
    private Track track = null!;
    private volatile bool trackFailed;
    private volatile bool trackCompleted;
    private bool trackCompletedLogged;
    private bool usingVirtualTrack;
    private double? loadStartedAt;
    private bool stopped;
    private bool audioReleased;

    // True on touchscreen platforms (Android/iOS): enables touch prompts and pausing when the app loses focus.
    // Desktop behaviour (mouse, Z/X, no automatic pause) is unaffected.
    private readonly bool touchPlatform = osu.Framework.RuntimeInfo.IsMobile;
    private readonly IBindable<bool> hostActive = new Bindable<bool>(true);

    private Container playfield = null!;
    private readonly List<DrawableHitCircle> active = new List<DrawableHitCircle>();
    private readonly Dictionary<int, DrawableHitCircle> byIndex = new Dictionary<int, DrawableHitCircle>();
    private int nextSpawn;

    private SpriteText scoreText = null!;
    private SpriteText accuracyText = null!;
    private SpriteText comboText = null!;
    private SpriteText progressText = null!;
    private SpriteText statusText = null!;
    private Box progressBar = null!;
    private HitErrorBar errorBar = null!;
    private Container resultsPanel = null!;
    private SpriteText resultsText = null!;

    public GameplayPlayer(Beatmap beatmap, double audioOffset)
    {
        this.beatmap = beatmap;
        this.audioOffset = audioOffset;
        RelativeSizeAxes = Axes.Both;
    }

    /// <summary>Current gameplay time in ms, or null before the clock has started.</summary>
    public double? GameplayTime => clock?.CurrentTime;

    public override bool HandleNonPositionalInput => true;

    public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

    [BackgroundDependencyLoader]
    private void load(ITrackStore tracks, GameHost host)
    {
        trackStore = tracks;
        // The judgement windows come from the beatmap's Overall Difficulty.
        engine = new GameplayEngine(beatmap);
        session = new GameplaySession(engine);
        engine.Judged += onJudged;

        var loaded = tracks.Get(beatmap.AudioFile);

        if (loaded == null)
        {
            Logger.Log($"[TaktChase] Audio track '{beatmap.AudioFile}' not found; playing silently.", LoggingTarget.Runtime, LogLevel.Important);
            loaded = tracks.GetVirtual(beatmap.LastObjectTime + 3000);
            usingVirtualTrack = true;
        }

        track = loaded;
        track.Failed += onTrackFailed;
        track.Completed += onTrackCompleted;

        InternalChildren = new Drawable[]
        {
            new Box { RelativeSizeAxes = Axes.Both, Colour = TaktPalette.BACKGROUND },
            playfield = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = Playfield.SIZE,
                Scale = new Vector2(2),
            },
            progressBar = new Box
            {
                RelativeSizeAxes = Axes.X,
                Width = 0,
                Height = 6,
                Colour = TaktPalette.CHASE,
            },
            // The HUD keeps clear of display cutouts and rounded corners. The padding comes from the host window's safe area
            // and is zero on desktop windows, so the desktop layout does not change.
            new SafeAreaContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    scoreText = text(Anchor.TopRight, 52, "Bold", new MarginPadding { Top = 24, Right = 40 }),
                    accuracyText = text(Anchor.TopRight, 28, null, new MarginPadding { Top = 86, Right = 40 }),
                    progressText = text(Anchor.TopLeft, 24, null, new MarginPadding { Top = 24, Left = 40 }),
                    comboText = text(Anchor.BottomLeft, 72, "Bold", new MarginPadding { Bottom = 24, Left = 40 }),
                    errorBar = new HitErrorBar(engine.Windows)
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Margin = new MarginPadding { Bottom = 40 },
                    },
                    statusText = text(Anchor.Centre, 40, "Bold", new MarginPadding()),
                },
            },
            resultsPanel = createResultsPanel(),
        };

        statusText.Text = "Loading audio...";
        refreshHud();

        // On touch platforms the game pauses itself when the window loses focus (notification shade, app switcher, home button).
        hostActive.BindTo(host.IsActive);
        hostActive.BindValueChanged(change =>
        {
            if (!change.NewValue && touchPlatform)
                Schedule(() => pause());
        });
    }

    // Raised on the audio thread: only set flags here.
    private void onTrackFailed() => trackFailed = true;

    private void onTrackCompleted() => trackCompleted = true;

    private SpriteText text(Anchor anchor, float size, string? weight, MarginPadding margin) => new SpriteText
    {
        Anchor = anchor,
        Origin = anchor,
        Margin = margin,
        Font = weight == null ? FontUsage.Default.With(size: size) : FontUsage.Default.With(size: size, weight: weight),
        Colour = TaktPalette.TEXT_PRIMARY,
    };

    private Container createResultsPanel()
    {
        resultsText = new SpriteText
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Font = FontUsage.Default.With(size: 34),
            Colour = TaktPalette.TEXT_PRIMARY,
            Spacing = new Vector2(0, 0),
        };

        return new Container
        {
            RelativeSizeAxes = Axes.Both,
            Alpha = 0,
            Children = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = new osuTK.Graphics.Color4(0, 0, 0, 200) },
                resultsText,
            },
        };
    }

    protected override void Update()
    {
        base.Update();

        if (stopped)
            return;

        if (clock == null)
        {
            tryStartClock();
            return;
        }

        clock.ProcessFrame();

        if (trackCompleted && !trackCompletedLogged)
        {
            trackCompletedLogged = true;
            Logger.Log("[TaktChase] Audio track ended.", LoggingTarget.Runtime, LogLevel.Verbose);
        }

        double time = clock.CurrentTime;

        session.Frame(time);
        spawnAndAnimate(time);
        refreshProgress(time);

        if (session.ShouldShowResults(time))
        {
            session.MarkResultsShown();
            showResults();
        }
    }

    private void tryStartClock()
    {
        loadStartedAt ??= Time.Current;

        bool timedOut = !track.IsLoaded && Time.Current - loadStartedAt.Value > load_timeout_ms;

        if (trackFailed || timedOut)
        {
            // Never block the game on audio: fall back to a silent virtual track, which still drives the clock.
            Logger.Log("[TaktChase] Audio track failed to load; playing silently.", LoggingTarget.Runtime, LogLevel.Important);
            releaseTrack();
            audioReleased = false;
            track = trackStore.GetVirtual(beatmap.LastObjectTime + 3000);
            track.Failed += onTrackFailed;
            track.Completed += onTrackCompleted;
            trackFailed = false;
            usingVirtualTrack = true;
        }
        else if (!track.IsLoaded)
            return;

        clock = new GameplayClock(track, audioOffset);
        clock.Start(beatmap.LeadInTime);
        statusText.Text = usingVirtualTrack ? "No audio" : string.Empty;
    }

    private void spawnAndAnimate(double time)
    {
        var objects = beatmap.HitObjects;

        while (nextSpawn < objects.Count && objects[nextSpawn].GetAppearTime(beatmap.Difficulty) <= time)
        {
            if (objects[nextSpawn] is HitCircle circle && !engine.IsJudged(nextSpawn))
            {
                var drawable = new DrawableHitCircle(circle, beatmap.Difficulty) { Depth = nextSpawn };
                playfield.Add(drawable);
                active.Add(drawable);
                byIndex[nextSpawn] = drawable;
            }

            nextSpawn++;
        }

        foreach (var drawable in active)
            drawable.UpdateTime(time);
    }

    private void onJudged(JudgementEvent judgement)
    {
        if (byIndex.Remove(judgement.Index, out var drawable))
        {
            active.Remove(drawable);
            drawable.ApplyResult(judgement.Result);
        }

        if (judgement.IsSuccessfulHit)
            errorBar.AddHit(judgement.TimingError!.Value, judgement.Result);

        spawnFeedback(judgement);
        refreshHud();
    }

    private void spawnFeedback(JudgementEvent judgement)
    {
        var label = new SpriteText
        {
            Anchor = Anchor.TopLeft,
            Origin = Anchor.Centre,
            Position = judgement.HitObject.Position - new Vector2(0, 30),
            Text = judgement.Result.DisplayName(),
            Font = FontUsage.Default.With(size: 16, weight: "Bold"),
            Colour = judgement.Result switch
            {
                HitResult.Great => TaktPalette.CHASE,
                HitResult.Ok => TaktPalette.OK,
                HitResult.Meh => TaktPalette.WARNING,
                _ => TaktPalette.TARGET,
            },
        };

        playfield.Add(label);
        label.MoveToOffset(new Vector2(0, -12), 450, Easing.OutQuint).FadeOut(450).Expire();
    }

    private int lastComboShown;

    private void refreshHud()
    {
        var score = engine.Score;

        scoreText.Text = score.Score.ToString("D8");
        accuracyText.Text = $"{score.Accuracy * 100:0.00}%";
        comboText.Text = score.Combo > 0 ? $"{score.Combo}x" : string.Empty;

        if (score.Combo > lastComboShown)
            comboText.ScaleTo(1.1f).Then().ScaleTo(1f, 120, Easing.OutQuint);

        lastComboShown = score.Combo;
        progressText.Text = $"{score.JudgedCount} / {engine.TotalObjects}";
    }

    private void refreshProgress(double time)
    {
        double start = beatmap.FirstObjectTime;
        double span = beatmap.LastObjectTime - start;
        progressBar.Width = span <= 0 ? 1 : (float)Math.Clamp((time - start) / span, 0, 1);
    }

    private void showResults()
    {
        var s = engine.Score;

        resultsText.Text =
            "MAP COMPLETE\n\n" +
            $"Score  {s.Score:N0}\nAccuracy  {s.Accuracy * 100:0.00}%\nMax combo  {s.MaxCombo}x\n" +
            $"300 {s.CountOf(HitResult.Great)}   100 {s.CountOf(HitResult.Ok)}   50 {s.CountOf(HitResult.Meh)}   Miss {s.CountOf(HitResult.Miss)}\n" +
            $"Mean error  {s.MeanHitError:+0.0;-0.0;0.0} ms   Deviation  {s.HitErrorDeviation:0.0} ms\n\n" +
            (touchPlatform ? "Tap to play again" : "Press R to play again");

        resultsPanel.FadeIn(400, Easing.OutQuint);
    }

    /// <summary>
    /// Sends one press to the session. The time is the clock's estimate for the moment the event reached the game (see
    /// <see cref="InputTimeEstimator"/>), not the previous frame's time.
    /// </summary>
    private void press(Vector2 screenSpacePointer)
    {
        if (stopped || clock == null || !clock.HasProcessedFrame)
            return;

        var pointer = playfield.ToLocalSpace(screenSpacePointer);
        session.Press(clock.EstimateCurrentTime(), pointer);
    }

    private string pausedText => touchPlatform ? "PAUSED\nTap to resume" : "PAUSED";

    private void togglePause()
    {
        if (stopped || clock == null)
            return;

        if (session.IsPaused)
            resume();
        else
            pause();
    }

    /// <summary>Pauses the session and the clock (and with it the audio track). Does nothing if already paused or finished.</summary>
    private void pause()
    {
        if (stopped || clock == null || !session.Pause())
            return;

        clock.Pause();
        statusText.Text = pausedText;
    }

    private void resume()
    {
        if (stopped || clock == null || !session.Resume())
            return;

        clock.Resume();
        statusText.Text = string.Empty;
    }

    protected override bool OnMouseDown(MouseDownEvent e)
    {
        press(e.ScreenSpaceMousePosition);
        return true;
    }

    /// <summary>
    /// Every finger is its own press. Handling the touch here (instead of letting the framework turn it into mouse input) matters:
    /// the framework maps all touches to ONE mouse button, so a second finger pressed while the first is still down would
    /// never produce a new press. Because this handler returns true, the framework does not also emit mouse events for the touch.
    /// </summary>
    protected override bool OnTouchDown(TouchDownEvent e)
    {
        switch (TouchInputMapper.Map(session))
        {
            case TouchAction.Restart:
                RestartRequested?.Invoke();
                break;

            case TouchAction.Resume:
                resume();
                break;

            default:
                press(e.ScreenSpaceTouch.Position);
                break;
        }

        return true;
    }

    protected override bool OnKeyDown(KeyDownEvent e)
    {
        if (e.Repeat || e.ControlPressed || e.AltPressed)
            return base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Z:
            case Key.X:
                // Keyboard presses use the current pointer position, exactly like a click.
                press(GetContainingInputManager().CurrentState.Mouse.Position);
                return true;

            case Key.Escape:
            case Key.Space:
                togglePause();
                return true;

            case Key.R:
                RestartRequested?.Invoke();
                return true;
        }

        return base.OnKeyDown(e);
    }

    /// <summary>
    /// Stops the session and releases the audio track immediately. Called before the player is replaced (restart), because
    /// disposal of a removed drawable is asynchronous and the old track must not keep playing under the new one.
    /// </summary>
    public void Stop()
    {
        stopped = true;
        releaseTrack();
    }

    private void releaseTrack()
    {
        if (audioReleased || track == null)
            return;

        audioReleased = true;
        track.Failed -= onTrackFailed;
        track.Completed -= onTrackCompleted;

        try
        {
            _ = track.StopAsync();
            (track as IDisposable)?.Dispose();
        }
        catch (Exception)
        {
            // The track may already be gone with its audio manager.
        }
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);

        if (!isDisposing)
            return;

        if (engine != null)
            engine.Judged -= onJudged;

        stopped = true;
        releaseTrack();
    }
}
